# AT&C Plugin - Contribution guidelines

Thank you for your interest in AT&C plugin, and for taking the time to contribute!

This document explains how the things currently work in our project, in a hope to make it easier for new contributors to navigate through the process.

With that, we are always open for suggestions. If you spot something that can be improved, please feel free to submit a pull request with proposed changes to this file.

## How the Process Goes

### Releases and `bleeding-edge` builds

- We do frequent incremental releases (a.k.a. "iterations" or "sprints").
- Every release adds more working functionality.
- During alpha and beta phases, the releases are numbered like "Alpha 2" 
- Upon every release, we update our download page at X-Plane.org with a ZIP for download, and the Release Notes 
- We use GitHub Milestones to track our work towards the releases (detailed in the next section)
- We try to release every 2-3 weeks, although we don't set any fixed deadlines.
- Between the releases, every change is immediately available as a `bleeding-edge` build
   - The latest `bleedinge-edge` build is available for download on the Releases page of the GitHub repository.
   - The `bleedinge-edge` build isn't manually tested, and as such, it can have unexpected problems
   
### Issues and Milestones

- All the work is managed through GitHub Issues.
- Issues are assigned to Milestones
- We have many release milestones and one backlog milestone
- A release milestone tracks work towards a specific release, and is named after that release, e.g. "Alpha 2"
- A special milestone named "Backlog" represents all filed issues that weren't yet planned for a release and are not worked on

### Labels
   
- Issues can be of different types, like bugs or new features. 
- We categorize the issues by tagging them with labels, in several aspects:
   - Issue type: like `bug` or `new-feature`
   - Epics: an issue can belong to one or more epics. Epic labels are prefixed with the word "epic", e.g. `epic-AIControllers`
   - Flags: like `community-request` or `not-programming`

### Release Planning 

When planning a new release, we:
1. Create a new release milestone, e.g. "Alpha 2"
1. Pick desired issues from the backlog.
   - To pick an issue from the backlog for a release, we change the milestone of the issue from "Backlog" to that of the release.
   
### Kanban Board

- We use one GitHub Project named "KANBAN" as the Kanban board for tracking issues that are worked on
- The Kanban board has the columns "TODO", "Dev In Progress", and "Test In Progress"
- Issues in the "TODO" column are ordered according to their priority (the most important are on the top)
- Before an issue in the "Dev In Progress" column can move to "Test In Progress", it should: 
   - have a merged pull request that resolves the issue
   - be closed
   - include a link to the above pull request
   - include a link to the bleeding edge release that contains the changes
- An issue stays in the "Test In Progress" column until it is tested by people other than the developer, and found to be resolved properly

## For Developers

### Setting up development environment

- Windows
- MacOS
- Linux

(TBD)

### Pull requests and continuous integration

- The code in the `master` branch is what's ready for release at any moment
- Changes should be submitted through pull requests
- Every pull request branch is built by a Continuous Integration (CI) pipeline implemented with GitHub Actions
- The CI pipeline does:
   - compile the plugin on Windows, Mac, and Linux
   - run the unit tests on each OS
   - package the plugin ZIP for download as a build artifact
   - *[PLANNED] run automation tests inside X-Plane on each OS*
- Every time a pull request is merged, the CI pipeline runs on the `master` branch
- When running on the `master` branch, the CI pipeline also publishes a `bleeding-edge` build for download

### Developer workflow

1. Pick an issue from the Kanban TODO column and move it to "Dev In Progress"
1. Do necessary development and initial testing
1. Create a pull request that resolves the issue
1. The maintainers will review the pull request and either merge it or request changes
1. When the pull request is merged, a new bleeding edge build containing the merged changes is automatically created
    - The latest bleeding edge build is available for download on the Releases page of the GitHub repository.  
1. Once the pull request is merged, the maintainers (or an automated process) will close the issue and move it to the "Test In Progress" column  
1. While in the "Test In Progress" column, the resolution of the issue may be tested by anyone who volunteers to do so
1. If with the issue resolution found to have a problem:
    - the problem should be well documented on the issue
    - the issue should be reopened, which automatically moves it back to the "TODO" column, and the workflow repeats
    - while in the "TODO" column, the issue can be picked by any developer, not necessarily the original one
   
