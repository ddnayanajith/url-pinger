# URL Pinger

A lightweight Windows desktop app that repeatedly sends a silent URL request on a user-selected interval.

The app is designed for a simple workflow:

- Set the target URL.
- Set the interval in seconds.
- Click **Start** to begin.
- Click **Stop** to stop.
- View request results in the activity log.

## Default URL

The app defaults to:

```text
https://oneapp.hutch.lk/
```

## Download / Run

Run the prebuilt executable:

```text
URLPinger.exe
```

No installation is required. The app uses `curl.exe` in the background, which is included by default on most Windows 10/11 systems.

## Build From Source

Run:

```cmd
build.cmd
```

This compiles `UrlPingerApp.cs` into `URLPinger.exe` using the .NET Framework C# compiler included with Windows.

## Last Result Meanings

| Output | Meaning |
| --- | --- |
| `No request yet` | Nothing has run yet. |
| `Running curl...` | One request is currently being sent. |
| `OK - curl exit 0` | The curl request completed successfully. |
| `FAIL - curl exit 7 - ...` | Curl could not connect to the server or network. |
| `FAIL - curl exit 28 - ...` | Usually means the request timed out. |
| `FAIL - curl exit [number] - ...` | Curl failed with that exit code and error text. |
| `FAIL - Timed out after [seconds] seconds` | The app killed curl because it exceeded the timeout setting. |
| `Stopped` | Appears in the log when Stop is clicked. |

## Creator

Created by [D D Nayanajith](https://github.com/ddnayanajith/).
