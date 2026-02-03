# ============================================================
# EGIBI REPO RESTRUCTURE — MIGRATION GUIDE (PowerShell)
# ============================================================
#
# PROBLEM: The .git repo is inside egibi-api/ but the .sln
# references SDK projects in sibling directories via ..\ paths.
# This means 6 SDK projects + db/ + Docker infra are unversioned.
#
# FIX: Move the git repo to the solution root (egibi/) so it
# tracks everything: API, SDKs, Docker infra, and db scripts.
#
# BEFORE:
#   egibi/                          ← no .git (nothing tracked here)
#   ├── EgibiBinanceUsSdk/          ← UNVERSIONED
#   ├── EgibiCoinbaseSDK/           ← UNVERSIONED
#   ├── EgibiCoreLibrary/           ← UNVERSIONED
#   ├── EgibiGeoDateTimeDataLibrary/ ← UNVERSIONED
#   ├── EgibiQuestDB/               ← UNVERSIONED
#   ├── EgibiStrategyLibrary/       ← UNVERSIONED
#   ├── api-config/                 ← STALE DUPLICATE
#   ├── db/                         ← UNVERSIONED
#   ├── egibi-api/                  ← .git HERE (only this dir tracked)
#   │   ├── .git/
#   │   ├── .gitignore              ← missing .NET ignores
#   │   ├── .vs/                    ← showing as untracked
#   │   ├── bin/                    ← showing as untracked
#   │   ├── obj/                    ← showing as untracked
#   │   ├── docker-compose.yml      ← references ./db/ which doesn't exist here
#   │   └── egibi-api.sln           ← references ..\ projects outside repo
#   ├── egibi-connections-manager/  ← separate .git
#   └── egibi-ui/                   ← NO .git at all
#
# AFTER:
#   egibi/                          ← .git HERE (tracks everything)
#   ├── .git/
#   ├── .gitignore                  ← comprehensive .NET + Docker + Node ignores
#   ├── .gitattributes              ← moved from egibi-api/
#   ├── .github/                    ← moved from egibi-api/
#   ├── egibi-api.sln               ← moved up, paths updated
#   ├── docker-compose.yml          ← moved up, ./db/ path now correct
#   ├── .env.example                ← moved up
#   ├── backup.sh                   ← moved up
#   ├── backup.ps1                  ← moved up
#   ├── restore.sh                  ← moved up
#   ├── README.md                   ← solution-level readme
#   ├── db/                         ← NOW VERSIONED
#   ├── egibi-api/                  ← just the API project code
#   ├── EgibiBinanceUsSdk/          ← NOW VERSIONED
#   ├── EgibiCoinbaseSDK/           ← NOW VERSIONED
#   ├── EgibiCoreLibrary/           ← NOW VERSIONED
#   ├── EgibiGeoDateTimeDataLibrary/ ← NOW VERSIONED
#   ├── EgibiQuestDB/               ← NOW VERSIONED
#   ├── EgibiStrategyLibrary/       ← NOW VERSIONED
#   └── egibi-connections-manager/  ← NOW VERSIONED (remove its .git)
#
#   egibi-ui/                       ← SEPARATE repo (init its own .git)
#
# ============================================================


# ============================================================
# STEP 0: SAFETY — Commit or stash any pending work
# ============================================================
# Do this from egibi/egibi-api/ (current repo root)
Set-Location egibi-api
git stash  # or git commit if you want to save current state
Set-Location ..


# ============================================================
# STEP 1: Move .git and repo-level files to solution root
# ============================================================
# From egibi/ directory:

# Move the git repo itself
Move-Item -Force egibi-api\.git .

# Move repo-level files
Move-Item -Force egibi-api\.gitattributes .
Move-Item -Force egibi-api\.github .

# Move Docker infrastructure files (these belong at solution root
# because docker-compose references ./db/ which is here)
Move-Item -Force egibi-api\docker-compose.yml .
Move-Item -Force egibi-api\.env.example .
Move-Item -Force egibi-api\backup.sh .
Move-Item -Force egibi-api\backup.ps1 .
Move-Item -Force egibi-api\restore.sh .

# The old .sln stays in egibi-api/ for now (we have a new one at root)
# Delete it after verifying the new one works:
# Remove-Item egibi-api\egibi-api.sln

# Delete the old incomplete .gitignore (replaced by root-level one)
Remove-Item egibi-api\.gitignore


# ============================================================
# STEP 2: Verify new files are in place
# ============================================================
# You should already have these files at the root (from the deliverable):
#   egibi-api.sln   ← new .sln with updated project paths
#   .gitignore       ← comprehensive .NET + Docker + Node ignores
#
# If not, copy them from the deliverable.


# ============================================================
# STEP 3: Clean up stale/duplicate items
# ============================================================

# Delete stale duplicate config directory
Remove-Item -Recurse -Force api-config

# Delete bin/obj/.vs build artifacts (they'll regenerate on next build)
Remove-Item -Recurse -Force egibi-api\bin, egibi-api\obj, egibi-api\.vs
Remove-Item -Recurse -Force EgibiBinanceUsSdk\bin, EgibiBinanceUsSdk\obj
Remove-Item -Recurse -Force EgibiCoinbaseSDK\bin, EgibiCoinbaseSDK\obj
Remove-Item -Recurse -Force EgibiCoreLibrary\bin, EgibiCoreLibrary\obj
Remove-Item -Recurse -Force EgibiGeoDateTimeDataLibrary\bin, EgibiGeoDateTimeDataLibrary\obj
Remove-Item -Recurse -Force EgibiQuestDB\bin, EgibiQuestDB\obj
Remove-Item -Recurse -Force EgibiStrategyLibrary\bin, EgibiStrategyLibrary\obj

# Remove egibi-connections-manager's separate .git (now part of main repo)
Remove-Item -Recurse -Force egibi-connections-manager\.git


# ============================================================
# STEP 4: Tell git about the restructure
# ============================================================

# Git now sees all the old egibi-api/ files as "moved" from root to subdirectory.
# We need to let git know about the new structure:

# First, untrack the old paths (git thinks files moved from root to egibi-api/)
git rm -r --cached .
# ^ This un-stages everything. Don't worry, no files are deleted.

# Re-add everything with the new structure
git add .

# Verify what git sees:
git status

# You should see:
#   renamed: Controllers/... -> egibi-api/Controllers/...
#   renamed: Program.cs -> egibi-api/Program.cs
#   new file: EgibiBinanceUsSdk/...  (SDK projects now tracked!)
#   new file: db/...                  (Docker init scripts now tracked!)
#   new file: docker-compose.yml      (at root level)
#   new file: .gitignore
#   new file: egibi-api.sln
#
# And you should NOT see any bin/, obj/, .vs/ files


# ============================================================
# STEP 5: Commit the restructure
# ============================================================
git commit -m "restructure: move repo root to solution level

- Move .git to solution root so all projects are versioned
- Move .sln to root with updated project paths
- Move Docker infrastructure (docker-compose, backup/restore) to root
- Add comprehensive .gitignore (.NET, Docker, Node, IDE)
- Remove stale api-config/ duplicate
- SDK projects (Binance, Coinbase, Core, etc.) now version controlled
- db/ init scripts now version controlled"


# ============================================================
# STEP 6: Open the solution in Visual Studio
# ============================================================
# Open egibi-api.sln from the solution root (egibi/)
# Visual Studio should find all projects since the GUIDs haven't changed.
# Delete the old egibi-api/egibi-api.sln once confirmed working:
# Remove-Item egibi-api\egibi-api.sln


# ============================================================
# STEP 7: Set up egibi-ui as a separate repo
# ============================================================
# egibi-ui is a standalone Angular project — it should be its own repo.
# It already has a proper .gitignore.
#
# Option A: Move it out of the solution directory
#   Move-Item egibi-ui ..\egibi-ui
#   Set-Location ..\egibi-ui
#   git init
#   git add .
#   git commit -m "initial commit: egibi-ui Angular 19 project"
#
# Option B: Keep it where it is but add to root .gitignore
#   Add-Content .gitignore "`negibi-ui/"
#   Set-Location egibi-ui
#   git init
#   git add .
#   git commit -m "initial commit: egibi-ui Angular 19 project"


# ============================================================
# STEP 8: Verify everything works
# ============================================================
# 1. Open solution in VS: egibi-api.sln (from root)
# 2. Build: dotnet build
# 3. Docker: docker compose up -d (from root — ./db/ path now correct)
# 4. Run API: Set-Location egibi-api; dotnet run
# 5. Run UI: Set-Location egibi-ui; ng serve
# 6. Check git: git status (should be clean after commit)
