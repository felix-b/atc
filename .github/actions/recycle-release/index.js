const core = require('@actions/core');
const github = require('@actions/github');

// const { Octokit } = require("@octokit/rest");
// const octokit = new Octokit();

const owner = github.context.repo.owner;
const repo = github.context.repo.repo;
const buildNumber = github.context.run_number;
const token = core.getInput('token', {required: true});
const tagBase = core.getInput('tag_base', {required: true});
const commitSha = github.context.sha;
const releaseNotes = core.getInput('release_notes', {required: true});
const downloadAssetName = core.getInput('download_asset_name', {required: true});

const newTagName = `${tagBase}-${buildNumber}`;

core.info(`ADVANCE-RELEASE> ${JSON.stringify({ 
    owner, 
    repo, 
    buildNumber, 
    token: typeof token, 
    tagBase, 
    commitSha, 
    releaseNotes, 
    downloadAssetName,
    newTagName 
}, null, 2)}`);

const client = new github.GitHub(token);

core.info(`ADVANCE-RELEASE> client initialized.`);

async function run() {
    try {
        const oldReleaseId = await tryFindOldReleaseId();

        if (oldReleaseId) {
            await deleteOldRelease(oldReleaseId);
        } else {
            core.info(`ADVANCE-RELEASE> old release not found, nothing to delete.`);
        }
        
        await createNewTag();

        core.setOutput('new_tag_name', newTagName);
        core.setOutput('new_release_body', formatBody());
    } catch (error) {
        core.error(error);
        core.setFailed(error.message);
    }
}

async function tryFindOldReleaseId() {
    core.info(`ADVANCE-RELEASE> looking for old release`);
    const releases = await client.repos.listReleases({
        owner,
        repo,
    })?.data;

    core.info(`ADVANCE-RELEASE> listReleases returned data type [${typeof releases}] with length=[${releases?.length}]`);
    
    const allMatchedReleases = releases?.filter(r => r.tag_name.indexOf(tagBase) === 0);
    const firstMatchedRelease = allMatchedReleases ? allMatchedReleases[0] : undefined;

    core.info(`ADVANCE-RELEASE> matched releases: length=[${allMatchedReleases?.length || 'N/A'}]`);
    core.info(`ADVANCE-RELEASE> first matched release id=[${matchedRelease?.id || 'N/A'}]`);

    return firstMatchedRelease?.id;
}

async function deleteOldRelease(releaseId) {
    core.info(`ADVANCE-RELEASE> deleting old release id=[${releaseId}]`);

    await client.repos.deleteRelease({
        owner,
        repo,
        releaseId
    });

    core.info(`ADVANCE-RELEASE> deleted old release.`);
}

async function createNewTag() {
    core.info(`ADVANCE-RELEASE> creating new tag [${newTagName}]`);
    await client.git.createRef({
        owner,
        repo,
        ref: `refs/tags/${newTagName}`,
        sha: commitSha
    });
    core.info(`ADVANCE-RELEASE> tag created.`);
}

function formatBody() {
    return (
        `## Build #${buildNumber} Release Notes\n\n` +
        `${releaseNotes}\n\n` +
        `### IMPORTANT!\n\n` +
        `This build contains the very latest changes that weren't yet released and weren't thoroughly tested (expect dragons!).\n\n` +
        `Make sure you backup your current version in case you'll want to revert to it.\n\n` +
        `Download this build: [${downloadAssetName}](https://github.com/${owner}/${repo}/releases/download/${newTagName}/${downloadAssetName})\n\n`
    );
}

run();
