# Slides: Next Stop, Virtual Actors

Slide deck for the .NET Stammtisch Linz talk (45 min including discussion). reveal.js 5.2.1 bundled with Vite and TypeScript, dark theme, no CDN, works offline.

## Run

```bash
npm install
npm run dev      # http://localhost:5173
npm run build    # output in dist/
npm run preview  # serve dist/
```

Keys: arrows to move, `S` opens the speaker view with notes and timer, `F` fullscreen, `O` overview, `?` help.

## Edit

- All slides are in `slides.md`. `---` starts a new slide, `--` starts a vertical (sub) slide, `Note:` starts the speaker notes.
- Diagram and image spots are marked with `<!-- TODO IMAGE: ... -->` and a dashed `placeholder` box. Put the file into `public/images/` (see the README there) and replace the box with `![alt](./images/name.svg)`.
- Slides marked PLANNED, NOT BUILT in the notes (versioning demo, Kubernetes, load test) are not implemented in the repo yet.
- Look and feel: `src/custom.css`. Settings (size, transition): `src/main.ts`.
- TODO placeholders to replace: slides URL, speaker photo.

## Import into Bolt or StackBlitz

Zip this folder without `node_modules` and `dist` and drag it into bolt.new, or push it to GitHub and open it via `https://stackblitz.com/github/<user>/<repo>/tree/<branch>/docs/net_stammtisch_linz/slides`. Bolt and StackBlitz run `npm install && npm run dev` for you.

## References on a slide

Write a green superscript number in the text and one `refs` block at the end of the slide. The block is shown at the bottom of the slide, above the footer:

```html
## Grain persistence<sup class="ref">1</sup>

<div class="refs"><sup class="ref">1</sup> <a href="https://example.com/page">https://example.com/page</a></div>
```

The link text is the URL itself, no title. For several sources put one `<div>` per source inside the `refs` block, numbered 1, 2, 3 per slide. At start `src/main.ts` renumbers all references across the deck by first appearance (the same URL keeps its number) and fills the closing "References" slides with the complete list. Keep the three placeholder slides at the end of `slides.md`; unused ones are removed.

## Export to PDF

Open `http://localhost:5173/?print-pdf` (also works on the `preview` server), then print from Chrome: destination "Save as PDF", layout landscape, margins none, enable "Background graphics". Add `&showNotes=true` to include the speaker notes.
