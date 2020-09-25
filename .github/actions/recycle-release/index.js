const core = require('@actions/core');
const github = require('@actions/github');

// const { Octokit } = require("@octokit/rest");
// const octokit = new Octokit();

const owner = github.context.repo.owner;
const repo = github.context.repo.repo;
const buildNumber = core.getInput('build_number', {required: true});
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

        core.setOutput('new_tag', newTagName);
        core.setOutput('new_release_body', formatBody());
    } catch (error) {
        core.error(error);
        core.setFailed(error.message);
    }
}

async function tryFindOldReleaseId() {
    core.info(`ADVANCE-RELEASE> looking for old release`);
    const releasesReply = await client.repos.listReleases({
        owner,
        repo,
    });
    const releases = releasesReply.data;

    //core.info(`ADVANCE-RELEASE> listReleases returned data type [${typeof releases}] with length=[${releases.length}]`);
    core.info(`ADVANCE-RELEASE> listReleases returned [${JSON.stringify(releases, null, 2)}]`);
    
    const allMatchedReleases = releases.filter(r => r.tag_name.indexOf(tagBase) === 0);
    const firstMatchedRelease = allMatchedReleases ? allMatchedReleases[0] : undefined;

    core.info(`ADVANCE-RELEASE> matched releases: length=[${allMatchedReleases.length || 'N/A'}]`);
    core.info(`ADVANCE-RELEASE> first matched release id=[${firstMatchedRelease ? firstMatchedRelease.id : 'N/A'}]`);

    return firstMatchedRelease ? firstMatchedRelease.id : undefined;
}

async function deleteOldRelease(releaseId) {
    core.info(`ADVANCE-RELEASE> deleting old release id=[${releaseId}]`);

    await client.repos.deleteRelease({
        owner,
        repo,
        release_id: releaseId
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
        `## Build # ${buildNumber} - Release Notes\n\n` +
        `Last change included in this build: ${releaseNotes}\n\n` +
        `### Important!\n` +
        `This build contains the very latest changes that weren't yet released and weren't thoroughly tested (expect dragons!).\n` +
        `Make sure you backup your current version in case you'll want to revert to it.\n\n` +
        `### Download build # ${buildNumber}\n` +
        `**[${downloadAssetName}](https://github.com/${owner}/${repo}/releases/download/${newTagName}/${downloadAssetName})**\n\n`
    );
}

run();
