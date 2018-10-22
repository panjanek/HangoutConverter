# HangoutConverter

Command line tool to convert google takeout archive files of hangouts conversation to beautiful PDF.

To get hangouts archive you have to log in to [https://takeout.google.com](https://takeout.google.com) create and download new archive. These archives tends to be big so uncheck all services and select only hangouts. After downloading and uncompressing archive file you should find the main archive in file Takeout\Hangouts\Hangouts.json. 
Then HangoutConverter can be used to generate PDF from Hangouts.json file. You have to specify name of two people whose conversations will be saved to PDF.

# Usage

```
HangoutConverter.exe <path-to-hangouts.json-file> "<name-of-first-person>" "<name-of-second-person>" <pdf-file-name> 
```

# Example

```
HangoutConverter.exe takeout-20181010T070302Z-001\Takeout\Hangouts\Hangouts.json "Rhett Buler" "Scarlett O'Hara" example.pdf
```

In effect you will get PDF file example.pdf looking like this:
![alt tag](https://raw.githubusercontent.com/panjanek/HangoutConverter/master/example/example.png) 

#Features

1. Supports very large archive files.
2. Embeds clickable links into PDF.
3. Embeds image thumbnails into PDF with link to full size image.
