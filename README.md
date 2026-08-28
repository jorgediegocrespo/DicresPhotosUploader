# Dicres Photos Uploader

Desktop app (Avalonia, .NET 10) for Windows and macOS that uploads all the
photos/videos from the subfolders of a root folder to Google Photos, creating
an album for each subfolder.

It can be run on demand from its interface, or configured to run on its own
in the background on the days/times you specify (using Windows Task
Scheduler or launchd on macOS): if the computer was off at that time, it
will run as soon as it turns back on/you log in.

It saves progress in `state.json` and **resumes exactly where it left off**
on each run, without re-uploading anything already uploaded.

> Looking to modify the code, add features, or fix a bug? See
> [ARCHITECTURE.md](ARCHITECTURE.md) for a full explanation of the project
> structure, entry point, and what each class does.

## 1. Requirements

- **.NET 10 SDK**. Check with `dotnet --version` (it should show 10.x). If
  you don't have it: `brew install --cask dotnet-sdk` (macOS) or download it
  from https://dotnet.microsoft.com/download/dotnet/10.0 (Windows).
- A (free) Google Cloud account to obtain OAuth credentials.

## 2. Configure Google Cloud (one time only)

1. Go to https://console.cloud.google.com/ and create a new project (or use an existing one).
2. Menu → **APIs & Services** → **Library** → search for **Google Photos Library API** → **Enable**.
3. Menu → **APIs & Services** → **OAuth consent screen**:
   - User type: **External**.
   - Fill in the app name, support email, and contact email (any of your own will do).
   - Under "Test users", add your own Gmail account.
   - No need to submit it for verification: since you're the only user, "Testing"
     mode is enough (you'll see an "app not verified" warning the first time you
     sign in; that's normal — click "Advanced" → "Go to [your app]").
4. Menu → **APIs & Services** → **Credentials** → **Create credentials** → **OAuth client ID**.
   - Application type: **Desktop app**.
   - Download the generated JSON and save it as `client_secret.json` in the
     project root: it's embedded into the assembly at build time, so the app
     never asks the user for it.

## 3. Where the configuration and state live

Everything (configuration, `state.json`, history, Google token, and logs of
scheduled runs) is saved per user in:

- macOS: `~/Library/Application Support/DicresPhotosUploader/`
- Windows: `%APPDATA%\DicresPhotosUploader\`

No need to touch anything by hand: it's all managed from the interface itself.

## 4. Running in development

```bash
dotnet run --project src/DicresPhotosUploader
```

This opens the window with 4 tabs:

- **Dashboard**: progress per album, "Run now" and "Reprocess errors"
  buttons, a filter to show only albums with errors, and a live log.
- **Configuration**: root folder, discarded files folder, batch size,
  allowed extensions, theme (System/Light/Dark), language
  (System/English/Español), and the "Reauthorize with Google" button (opens
  the browser to sign in; only needed once, and it doesn't request
  permission to read your library, only to **add** photos). The
  `client_secret.json` OAuth credentials ship embedded in the app, so
  there's nothing to select. The rest of the tabs stay locked until this one
  is complete (valid root folder + Google account authorized).
- **Schedule**: days of the week + time, and the "Enable background
  execution" switch. When saved, it registers the task in Task Scheduler
  (Windows) or the LaunchAgent (macOS). Requires having signed in with
  Google at least once from Configuration.
- **History**: past runs (manual and scheduled) with their result.

There is also an **About** entry in the application menu (the app menu on
macOS) showing the version and links to the repository and the product page.

### 4.1 Language

The interface and the run logs are available in **English** and **Spanish**.
By default the app follows the operating system's language ("System"); you
can force one from the Configuration tab. The change takes effect **the next
time you start the app**.

## 5. Publishing the standalone app (.exe / .app)

It is not published to any store: these are standalone, self-contained
executables (they don't require .NET to be installed on the target machine).

- **Windows** (run on Windows): `./scripts/build-windows-exe.ps1` →
  generates `dist/windows/DicresPhotosUploader.exe`.
- **macOS** (run on a Mac): `./scripts/build-macos-app.sh` → generates
  `dist/macos/osx-arm64/DicresPhotosUploader.app` and `dist/macos/osx-x64/DicresPhotosUploader.app`.
  The app is not signed (there's no Apple Developer account): the first
  time, right-click → **Open** to bypass the Gatekeeper warning.

Important: save the schedule (Schedule tab) **after** moving the app to its
final location, because the scheduled task points to the executable's path
at the time it's saved.

## 6. Notes about background execution

- The task/agent runs **only when you're logged in** on the computer (it
  doesn't require storing your password). If the machine was off or asleep
  at the scheduled time, it will run as soon as it becomes available again
  (Windows: "run as soon as possible"; macOS: launchd recovers pending
  `StartCalendarInterval` triggers when you log in).
- Scheduled runs run without a window (headless) and their log is saved to
  `logs/run-*.log` inside the app's data folder; their result also appears
  in the History tab.
- Only one run can happen at a time (manual or scheduled): if they overlap,
  the second one is silently cancelled and does nothing.
- No daily limit is deliberately configured: the upload keeps going until
  Google responds with a 429 (quota exhausted, ~10,000 requests/day); in
  that case the state is saved and the next run (manual or scheduled)
  continues where it left off.

## 7. Photos that fail to upload

If a photo fails to upload, the application discards it right away (there
are no automatic retries):

1. It won't be attempted again in future runs (it's recorded in
   `state.json`).
2. It **copies** the original file to `<ErroredFolderPath>/<AlbumName>/<file>`
   (for example: `errored/Summer 2019/IMG_045.jpg`).
3. The original file on your disk **is never touched**: it stays exactly
   where it always was in your photos folder. What's in `errored/` is just
   a copy so you can review it, open it, or try uploading it by hand later.

Check the run log (Dashboard/History, or the `errored/` folder) to see which
photos are pending manual review. At the end of every run the log includes a
list of the files that failed and why.

Use the **"Reprocess errors"** button on the Dashboard to retry everything
in the `errored/` folder: files that upload successfully are removed from
that folder and marked as uploaded (so a normal run won't try them again),
and the ones that still fail are left there for you to review.

## 8. Expected root folder structure

```
/Users/jorge/Photos
 ├─ Summer 2019/
 │   ├─ IMG_001.jpg
 │   └─ IMG_002.jpg
 ├─ Ana and Luis's Wedding/
 │   └─ ...
 └─ Mom's Birthday/
     └─ ...
```

Each top-level folder becomes an album with that same name. Files must be
directly inside each folder (Google Photos doesn't support nested albums,
so subfolders inside subfolders are not processed).

## 9. Notes

- Uploaded photos count against your Google account's storage
  (they're uploaded in original quality).
- Don't delete the app's data folder (see item 3) between runs or
  you'll lose your session/progress.

## 10. Automatic releases on `master`

This repository includes a GitHub Actions workflow at
`.github/workflows/release-on-master-push.yml` that creates a new GitHub
Release on every push to the `master` branch.

Each release gets an auto-generated tag in this format:
`release-<run_number>-<run_attempt>-<short_sha>`.

## 11. Tests are required to merge a pull request

The workflow at `.github/workflows/tests-on-pull-request.yml` (job name
`Run tests`) restores, builds and runs the xUnit test suite on every pull
request targeting `main` and on every push to `main`.

The `main` branch is protected and `Run tests` is configured as a required
status check, so a pull request cannot be merged until the whole test suite
passes. Branches must also be up to date with `main` before merging.

Run the same checks locally with:

```bash
dotnet test DicresPhotosUploader.slnx --configuration Release
```
