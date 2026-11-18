# Images and diagrams

Drop files here (PNG, SVG, JPG, WebP). Vite serves this folder as-is, so a file `public/images/hbf.svg` is used in `slides.md` as:

```markdown
![Hauptbahnhof is hot](./images/hbf.svg)
```

Tips:
- Prefer SVG for diagrams (sharp on a projector). Excalidraw can export SVG. The Mermaid sources are in `ARCHITECTURE.md` and `docs/net_stammtisch_linz_presentation/how-orleans-works.md` of the repo.
- Speaker photo: square, at least 800 x 800 px. QR code for the audience page: SVG or PNG, high contrast.
- Planned file names for the intro and versioning slides:
  - `tram_hbf.jpg`: photo of a Linz tram at Hauptbahnhof
  - `map_projector.png`: screenshot of the projector page (map with stops as dots). Keep "© OpenStreetMap contributors" visible.
  - `aspire_replicas.png`: Aspire dashboard with 3 gleis-grain replicas
  - `qr.svg`: QR code of the audience page
  - `map_legend.png`: legend, red = v1, blue = v2, shape = silo
  - `aspire_v1_v2.png`: Aspire dashboard with `gleis-grain-v1` and `gleis-grain-v2`
  - `rolling_update.svg`: timeline of a rolling update
- Every spot waiting for an image is marked in `slides.md` with `<!-- TODO IMAGE: ... -->` and a dashed `placeholder` box.
