# Lyrics CLI

A simple terminal lyrics player with synchronized lyrics,
MPRIS support, and animated line and word modes.

## Features

* Synchronized lyrics
* Line and word modes
* Animated lyrics
* MPRIS support
* Automatic lyrics fetching

## Requirements

* Linux
* .NET 10
* A media player with MPRIS support

## Installation

Clone the repository:

```bash
git clone https://github.com/d1shburn/lyrics-cli.git
cd lyrics-cli
```

Build and run:

```bash
dotnet run -c Release
```

### Global Installation

Publish the application:

```bash
dotnet publish -c Release -o ~/.local/share/lyrics
```

Add `lyrics` to your local binaries:

```bash
mkdir -p ~/.local/bin
ln -sf ~/.local/share/lyrics/Lyrics ~/.local/bin/lyrics
```

Make sure `~/.local/bin` is in your `PATH`.

Run from anywhere:

```bash
lyrics
```

## Usage

Default mode:

```bash
lyrics
```

Line mode:

```bash
lyrics --mode line
```

Word mode:

```bash
lyrics --mode word
```

Set the number of visible lines:

```bash
lyrics --lines 7
```

Combine options:

```bash
lyrics --mode line --lines 7
```

## Options

| Option             | Description                |
| ------------------ | -------------------------- |
| `--mode line`      | Display synchronized lines |
| `--mode word`      | Display synchronized words |
| `--lines <number>` | Set visible lines          |

## Lyrics Sources

Lyrics are fetched automatically from:

* LrcMux
* LRCLIB

No API keys or configuration are required.