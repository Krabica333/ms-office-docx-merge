# DocxMerge

A simple, free Windows app for combining DOCX documents. Drag your files into the window, arrange them in the order you want, and save them as one document.

Built with a minimal interface for people who just want to get the job done—no account, subscription, evaluation license, or Python script required.

## Features

- Drag and drop `.docx` files into the app
- Drag documents to change their order
- Remove a document with one click
- Merge and save with a single button
- Slovak interface with subtle animations
- Use your own PNG logo in the app and ICO icon for the executable
- Processes documents locally and leaves the original files unchanged

## Getting started

Download the Windows x64 executable from the **Releases** page, if one is available. Open the app, add at least two DOCX files, arrange them from top to bottom, then click **Zlúčiť a uložiť** (“Merge and save”).

Only `.docx` files are supported. The app does not convert older `.doc` files.

## Build from source

1. Install the .NET 10 SDK.
2. Download or clone this repository.
3. Optionally place `logo.png` and a valid `app.ico` in the project folder.
4. Run `Build.bat`.

The standalone Windows x64 executable will be created in `publish-x64`.

## Important note about formatting

DocxMerge aims to preserve document content and section information, but DOCX files can contain complex styles, headers, fields, and layouts. **Always review the merged result**, especially before sharing or printing important documents.

## Credits

Created by Seb Matt — [krabicahub.xyz](https://www.krabicahub.xyz)

© 2026 Seb Matt www.krabicahub.xyz
