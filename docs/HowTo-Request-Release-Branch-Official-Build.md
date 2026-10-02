How To Request An Official Build From Release Branch
====================================================

This document provides the guidelines and procedure to patch a release branch for fixing quality issue identified in
production, and request a new official build including Nuget packages (a.k.a. nupkg).

Basic Guidelines
----------------

- Always commit the changes to master branch, then cherry-pick the change to release branch after passing the
  verification test. Unless it is technically impossible to do so, **DO NOT** commit changes to release branch directly.

- Always publish the nupkgs after the official build is completed and basic verfication is performed. Do not publish
  nupkgs during the build.

- Only publish the packages necessary for supporting partner teams and other source repos, not more.  We support all
  packages that we have published.

- Do not manually change the version in release branch. Let the build system to do it automatically.

Steps
-----

Firstly determines which release branch we want to generate the build. Let's say it is `release_1_5`. Check out the
branch by `git checkout release_1_5` and pull the latest change by `git pull`.

Then cherry-pick the required changes from master branch by `git cherry-pick [CommitId]`.  Perform a clean build
locally, as well as some smoking test if necessary.

Push to remote server by `git push`.

Request an official build by `buildreq --postbuildproject WAES.proj`.

After the build is completed, run some verification test to ensure the build is good. Then publish the required nupkgs
by submitting the BuildTracker job at [WABT](http://wabt/BuildTracker/Jobs/BuildRequest.aspx?Id=0), select the following
parameters:

* Product: `Git - Build`
* Branch: `Generic_Jobs`
* Job: `PublishOfficialBuildPackages`
* DropPath: build drop path on reddog where packages directory is located, e.g.
  `git_networking_vega_master\1.6.0.5\packages`
* SingleNupkgFilename: name of nupkg, e.g. `VegaBackend-master.1.6.0.5.nupkg`.

Multiple jobs may be submitted if more than more nupkg is required to be published.

Releae Branch Names
-------------------

Prior to (excluding) release 1.6, release branch names are like `release_1_5`, `release_1_4`. Since release 1.6, branch
names use more readable forms like `release/1.6`. During the build, special characters are replaced with underscores.
