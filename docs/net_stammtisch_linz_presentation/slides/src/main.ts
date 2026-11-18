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

// References: a slide writes <sup class="ref">1</sup> in its text and one <div class="refs"> with the sources.
// The div is hidden inside the slide and shown here, at the bottom above the footer, so it stays in the same place on every slide.
const refs = document.createElement("div");
refs.className = "deck-refs";
document.querySelector(".reveal")?.appendChild(refs);

const showRefs = (slide?: Element) => {
  const source = slide?.querySelector(".refs");
  refs.innerHTML = source ? source.innerHTML : "";
};

// Numbers the references across the whole deck by first appearance. A slide keeps writing its own 1, 2 in the text and in
// its refs block; here each URL gets one deck-wide number (the same URL keeps the same number) in the text and the block.
const numberReferences = (): Map<string, number> => {
  const numbers = new Map<string, number>();
  const slides = Array.from(document.querySelectorAll<HTMLElement>(".slides section")).filter((section) => !section.querySelector("section"));
  for (const slide of slides) {
    const block = slide.querySelector<HTMLElement>(".refs");
    if (!block) continue;
    const entries = block.querySelector(":scope > div") ? Array.from(block.querySelectorAll<HTMLElement>(":scope > div")) : [block];
    const localToGlobal = new Map<string, number>();
    for (const entry of entries) {
      const url = entry.querySelector("a")?.getAttribute("href");
      const marker = entry.querySelector("sup.ref");
      if (!url || !marker) continue;
      if (!numbers.has(url)) numbers.set(url, numbers.size + 1);
      const global = numbers.get(url) ?? 0;
      localToGlobal.set(marker.textContent ?? "", global);
      marker.textContent = String(global);
    }
    slide.querySelectorAll<HTMLElement>("sup.ref").forEach((marker) => {
      const global = block.contains(marker) ? undefined : localToGlobal.get(marker.textContent ?? "");
      if (global !== undefined) marker.textContent = String(global);
    });
  }
  return numbers;
};

// The closing slides list all references. They are placeholders in slides.md (div.allrefs with data-part); unused ones are removed.
const fillReferenceSlides = (numbers: Map<string, number>) => {
  const perSlide = 14;
  const containers = Array.from(document.querySelectorAll<HTMLElement>(".allrefs"));
  const entries = Array.from(numbers.entries());
  const parts = Math.max(1, Math.ceil(entries.length / perSlide));
  containers.forEach((container, index) => {
    const slide = container.closest("section");
    if (index >= parts) {
      slide?.remove();
      return;
    }
    container.innerHTML = entries
      .slice(index * perSlide, (index + 1) * perSlide)
      .map(([url, n]) => `<div><sup class="ref">${n}</sup> <a href="${url}">${url}</a></div>`)
      .join("");
    const heading = slide?.querySelector("h2");
    if (heading) heading.textContent = parts > 1 ? `References (${index + 1}/${parts})` : "References";
  });
};

type Hljs = { highlight: (code: string, options: { language: string }) => { value: string } };
const highlightPlugin = (Highlight as unknown as () => { hljs: Hljs })();

const reveal = new Reveal({
  width: 1280,
  height: 720,
  hash: true,
  slideNumber: "c/t",
  transition: "fade",
  controls: true,
  progress: true,
  plugins: [Markdown, Notes, highlightPlugin as unknown as typeof Markdown],
});

reveal.on("ready", (event) => {
  fillReferenceSlides(numberReferences());
  reveal.sync();
  showRefs((event as unknown as { currentSlide: Element }).currentSlide);
});
reveal.on("slidechanged", (event) => showRefs((event as unknown as { currentSlide: Element }).currentSlide));
// Line by line highlighting, so a line can carry a fragment (a rectangle on click) and still get syntax colours.
const highlightLines = () => {
  document.querySelectorAll("pre.cs .l").forEach((line) => {
    line.innerHTML = highlightPlugin.hljs.highlight(line.textContent ?? "", { language: "csharp" }).value;
  });
};
// Tall code blocks get a class, so CSS can make them smaller instead of letting them scroll or overflow.
const markTallCode = () => {
  document.querySelectorAll("pre code").forEach((code) => {
    const lines = (code.textContent ?? "").trim().split("\n").length;
    if (lines >= 15) code.closest("pre")?.classList.add("tall");
  });
};
reveal.on("ready", markTallCode);
reveal.on("ready", highlightLines);
reveal.initialize();
