# File System Scan

File System utility to scan for files recursively, calculate checksum and export to JSON. Then find the difference between two directory structures.

Originally created to scan for changes between two disks with parallel copies. The solution helped Agama Yoga few times when solving issues with old disks and archived data.

Made with .NET Framework 2.0 for Windows platform. A command line utility for technical staff.

## Getting Started

Run `make.bat` to build

Run `fsscan --help` for more info

## Examples

### Scan

Scan the local directory
```bash
fsscan -s c:\Projects\fsscan -o dir1.json
```

Console output
```
Scanning c:\Projects\fsscan (446.9GB)

    79.2KB         34 [========================================] 100.0% -

Found 34 files and folders, 0 errors, 0.0% of drive space

Done!
```

Content of `dir1.json`
```json
[
  {
    "path": "c:\\Projects\\fsscan",
    "dir": true,
    "created": "2020-02-18T01:36:07Z",
    "modified": "2020-05-02T04:54:19Z",
    "accessed": "2020-05-02T04:54:19Z"
  },
  {
    "path": "c:\\Projects\\fsscan\\.gitignore",
    "sha1": "506f464608a87c22cf7020f2c56b972295bd76c9",
    "length": 2876,
    "created": "2020-02-18T01:36:27Z",
    "modified": "2020-05-02T04:41:56Z",
    "accessed": "2020-02-18T01:36:27Z"
  },
  {
    "path": "c:\\Projects\\fsscan\\make.bat",
    "sha1": "221614361d4a0c0402161d890454e5d269475bfc",
    "length": 625,
    "created": "2020-02-18T01:36:27Z",
    "modified": "2020-05-02T04:46:45Z",
    "accessed": "2020-02-18T01:36:27Z"
  },
  {
    "path": "c:\\Projects\\fsscan\\README.md",
    "sha1": "97bfde9768520bffd918223bba65506520e24f91",
    "length": 196,
    "created": "2020-02-18T01:37:23Z",
    "modified": "2020-05-02T04:43:05Z",
    "accessed": "2020-02-18T01:37:23Z"
  }
]
```

### Compare

To compare two JSON files
```
fsscan -c dir1.json dir2.json -o diff.json
```

```
Comparing...
A: 34 records
B: 34 records

    79.2KB         34 [========================================] 100.0% -

Found 1 conflicts

Done!
```

Content of `diff.json`
```json
[
  {
    "path": "c:\\Projects\\fsscan\\README.md",
    "message": "File size mismatch!"
  }
]
```