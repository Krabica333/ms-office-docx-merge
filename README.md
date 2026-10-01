# DocxMerge

<a href="https://apps.microsoft.com/detail/9MWH9KSG5MCB?referrer=appbadge&mode=full" target="_blank"  rel="noopener noreferrer">
	<img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

DocxMerge is a free, open-source Windows app that combines multiple DOCX documents into one file. Add or drag in your documents, arrange them in the order you want, and save the result.

Documents are processed locally. Your original files are not changed.

## Features

- Simple English interface
- Drag and drop DOCX files into the window
- Drag documents in the list to change their order
- Remove a document with the `×` beside its name
- Subtle interface animations
- Optional custom logo and executable icon
- No Python, Aspose.Words, subscription, or evaluation license
- No DOC-to-DOCX converter

## Download and use

1. Download the Windows x64 application ZIP from the [Releases](../../releases) page.
2. Extract the ZIP.
3. Open `DocxMerge.exe`.
4. Add at least two `.docx` files.
5. Drag their names into the order you want. Documents merge from top to bottom.
6. Click **Merge and save** and choose where to save the result.

The published self-contained build does not require a separate .NET installation. The provided build is for x64 Windows computers.

### Windows SmartScreen notice

Windows may show **“Windows protected your PC”** or call DocxMerge an **“unrecognized app”** because the downloadable executable is new and unsigned. This is a reputation warning, not a request for a software license.

Download the app only from this repository’s [Releases](../../releases) page. If you trust the file you downloaded, you can select **More info** → **Run anyway**. Do not disable SmartScreen for your entire computer.

If Windows reports that the file contains malware, rather than simply calling it unrecognized, do not run it.
> **Check the result:** Complex Word formatting, such as headers, page layouts, fields, or conflicting styles, may change during merging. Review important documents before using or sharing them.

## Build from source

Building requires the .NET 10 SDK on Windows. Open a terminal in the project folder and run:

```bat
dotnet restore DocxMerge.csproj --configfile NuGet.Config
dotnet publish DocxMerge.csproj -c Release -r win-x64 --self-contained true -o publish-x64
```

The finished executable will be in `publish-x64`.

You can also double-click `Build.bat` if it is included in your source download.

### Custom images

To display your logo inside the app, place a PNG named `logo.png` beside `DocxMerge.csproj` before building.

To set the executable’s icon, place a genuine Windows icon file named `app.ico` beside `DocxMerge.csproj` before building. Renaming a PNG to `.ico` is not enough.

Both image files are optional.

## Screenshots

<img width="1918" height="1027" alt="Screenshot 2026-10-01 020847" src="https://github.com/user-attachments/assets/9504ae70-d420-48df-b400-1bcc8b55bb28" />
<img width="1917" height="1030" alt="Screenshot 2026-10-01 020908" src="https://github.com/user-attachments/assets/94911104-a495-44ec-83a7-cbab7e061818" />


## Limitations

- Input files must be `.docx`; legacy `.doc` files are not supported.
- Perfect preservation of every DOCX feature cannot be guaranteed.
- This repository provides the Windows x64 build instructions. Other processor architectures require a separate publish build.

## License

DocxMerge's original source code is licensed under the MIT License. See [LICENSE](LICENSE).

Third-party dependencies retain their own licenses. The MIT License for DocxMerge does not replace those licenses. Make sure you have the right to redistribute any custom logo or icon you include.

## AI Development Transparency

In the spirit of open-source transparency, this application was built using AI assistance

## Credits

© 2026 Seb Matt [www.krabicahub.xyz](https://www.krabicahub.xyz)

DocxMerge uses third-party open-source dependencies. Their licenses remain applicable when the app is redistributed.
