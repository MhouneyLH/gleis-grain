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
- TODO placeholders to replace: audience QR code and URL, slides URL, speaker photo.

## Import into Bolt or StackBlitz

Zip this folder without `node_modules` and `dist` and drag it into bolt.new, or push it to GitHub and open it via `https://stackblitz.com/github/<user>/<repo>/tree/<branch>/docs/net_stammtisch_linz/slides`. Bolt and StackBlitz run `npm install && npm run dev` for you.

## Export to PDF

Open `http://localhost:5173/?print-pdf` (also works on the `preview` server), then print from Chrome: destination "Save as PDF", layout landscape, margins none, enable "Background graphics". Add `&showNotes=true` to include the speaker notes.
