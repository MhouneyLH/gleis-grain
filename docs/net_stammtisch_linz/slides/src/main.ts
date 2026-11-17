import Reveal from "reveal.js";
import Markdown from "reveal.js/plugin/markdown/markdown.esm.js";
import Notes from "reveal.js/plugin/notes/notes.esm.js";
import Highlight from "reveal.js/plugin/highlight/highlight.esm.js";

import "reveal.js/dist/reset.css";
import "reveal.js/dist/reveal.css";
import "reveal.js/dist/theme/black.css";
import "reveal.js/plugin/highlight/monokai.css";
import "./custom.css";

// Footer on every slide. Set the date of the event here.
const FOOTER = {
  event: ".NET Stammtisch Linz",
  date: "18.11.2026",
  speakers: "Lucas Hünniger & Raffael Rehberger",
};

// All slide content lives in slides.md, imported as plain text.
import slides from "../slides.md?raw";

const deck = document.getElementById("deck") as HTMLElement;
const template = document.createElement("textarea");
template.setAttribute("data-template", "");
template.textContent = slides;
deck.appendChild(template);

const footer = document.createElement("div");
footer.className = "deck-footer";
footer.textContent = [FOOTER.event, FOOTER.date, FOOTER.speakers].join("  ·  ");
document.querySelector(".reveal")?.appendChild(footer);

const reveal = new Reveal({
  width: 1280,
  height: 720,
  hash: true,
  slideNumber: "c/t",
  transition: "fade",
  controls: true,
  progress: true,
  plugins: [Markdown, Notes, Highlight],
});

reveal.initialize();
