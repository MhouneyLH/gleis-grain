#!/usr/bin/env python3
"""Generates the .excalidraw files for the concurrency part of the Orleans talk.
Run: python3 generate.py   (writes next to this script)."""
import json, os, random

OUT = os.path.dirname(os.path.abspath(__file__))
BG = "#0b1220"
GREY = "#cfd6e4"; DIM = "#6b7690"
GREEN = "#2f9e44"; BGREEN = "#51cf66"; ORANGE = "#e8590c"; YEL = "#f08c00"
RED = "#ff4d6d"; PURPLE = "#b58cff"; CYAN = "#22b8cf"; BLUE = "#4dabf7"
FONT = 5


class Canvas:
    def __init__(self):
        self.els = []
        self.n = 0
        random.seed(7)

    def _base(self, typ, x, y, w, h, stroke, bg="transparent", sw=2, dash="solid", rough=1, rounded=True):
        self.n += 1
        e = {"id": f"el{self.n:04d}", "type": typ, "x": x, "y": y, "width": w, "height": h,
             "angle": 0, "strokeColor": stroke, "backgroundColor": bg, "fillStyle": "solid",
             "strokeWidth": sw, "strokeStyle": dash, "roughness": rough, "opacity": 100,
             "groupIds": [], "frameId": None, "index": f"a{self.n:04d}",
             "roundness": {"type": 3} if (rounded and typ in ("rectangle", "diamond", "ellipse")) else
                          ({"type": 2} if typ in ("arrow", "line") else None),
             "seed": random.randint(1, 2**31 - 1), "version": 1,
             "versionNonce": random.randint(1, 2**31 - 1), "isDeleted": False,
             "boundElements": None, "updated": 1700000000000, "link": None, "locked": False}
        if typ == "ellipse":
            e["roundness"] = {"type": 2}
        self.els.append(e)
        return e

    def rect(self, x, y, w, h, stroke=GREY, bg="transparent", sw=2, dash="solid", kind="rectangle"):
        return self._base(kind, x, y, w, h, stroke, bg, sw, dash)

    def text(self, cx, cy, s, size=24, color=GREY, align="center"):
        lines = s.split("\n")
        w = max(len(l) for l in lines) * size * 0.58
        h = len(lines) * size * 1.25
        x = cx - w / 2 if align == "center" else cx
        e = self._base("text", x, cy - h / 2, w, h, color, rounded=False)
        e.update({"text": s, "fontSize": size, "fontFamily": FONT, "textAlign": align,
                  "verticalAlign": "middle", "containerId": None, "originalText": s,
                  "autoResize": True, "lineHeight": 1.25, "baseFontSize": None, "labelPosition": None})
        return e

    def box(self, x, y, w, h, label, stroke=GREY, size=24, bg="transparent", tcolor=None, dash="solid", sw=2, kind="rectangle"):
        self.rect(x, y, w, h, stroke, bg, sw, dash, kind)
        self.text(x + w / 2, y + h / 2, label, size, tcolor or stroke)

    def arrow(self, pts, color=GREY, sw=3, dash="solid", head="arrow", tail=None, kind="arrow"):
        x0, y0 = pts[0]
        rel = [[px - x0, py - y0] for px, py in pts]
        w = max(abs(p[0]) for p in rel); h = max(abs(p[1]) for p in rel)
        e = self._base(kind, x0, y0, w, h, color, sw=sw, dash=dash)
        e.update({"points": rel, "lastCommittedPoint": None, "startBinding": None, "endBinding": None,
                  "startArrowhead": tail, "endArrowhead": head if kind == "arrow" else None, "elbowed": False})
        if kind == "line":
            e.pop("lastCommittedPoint"); e.pop("startBinding"); e.pop("endBinding"); e.pop("elbowed")
            e["startArrowhead"] = None; e["polygon"] = False
        return e

    def line(self, pts, color=GREY, sw=2, dash="solid"):
        return self.arrow(pts, color, sw, dash, kind="line")

    def msg(self, x1, x2, y, label, color, dash="solid", size=22, sw=3, dy=-22):
        """Sequence message arrow with label above its centre."""
        self.arrow([(x1, y), (x2, y)], color, sw, dash)
        self.text((x1 + x2) / 2, y + dy, label, size, color)

    def save(self, name, w=1280, h=720):
        doc = {"type": "excalidraw", "version": 2, "source": "gleis-grain generate.py",
               "elements": self.els,
               "appState": {"gridSize": None, "viewBackgroundColor": BG},
               "files": {}}
        with open(os.path.join(OUT, name), "w") as f:
            json.dump(doc, f, indent=1)


def lifeline_head(c, x, label, top=24, bot=640, w=260, color=GREY, bw=None):
    c.box(x - w / 2, top, w, 76, label, color, 28)
    c.line([(x, top + 76), (x, bot)], DIM, 2)
    c.box(x - w / 2, bot, w, 60, label, color, 24)


def bar(c, x, y1, y2, color, w=22):
    c.rect(x - w / 2, y1, w, y2 - y1, color, "transparent", 3)


# ---------------- 01 deadlock ----------------
def deadlock():
    c = Canvas(); A, B = 330, 950
    lifeline_head(c, A, "StopGrain", color=GREY); lifeline_head(c, B, "TripGrain")
    bar(c, A, 150, 560, GREEN)
    c.text(A - 40, 150, "Stop turn", 20, GREEN, "center") if False else None
    c.msg(A + 11, B - 11, 190, "GetDelayAsync()  (Stop awaits)", GREEN)
    bar(c, B, 190, 470, GREEN)
    c.msg(B - 11, A + 11, 310, "GetBoardAsync()  call back", RED, sw=4)
    c.box(A + 20, 330, 250, 70, "waits in queue", ORANGE, 22, dash="dashed")
    c.text(A - 20, 230, "Stop turn not\nfinished: busy", 20, GREEN, "center") if False else None
    c.text(660, 440, "Stop waits for Trip,\nTrip waits for Stop", 26, RED)
    c.arrow([(A + 110, 400), (A + 110, 450), (B - 110, 450), (B - 110, 400)], RED, 4, "dashed", head=None) if False else None
    c.box(430, 505, 420, 80, "timeout after 30 s", RED, 34, sw=4)
    c.text(640, 600, "deadlock", 24, RED) if False else None
    c.text(1150, 300, "X", 60, RED) if False else None
    c.save("01-deadlock.excalidraw")


# ---------------- 02 reentrant ----------------
def reentrant():
    c = Canvas(); A, B = 330, 950
    lifeline_head(c, A, "StopGrain [Reentrant]", w=340); lifeline_head(c, B, "TripGrain")
    bar(c, A, 150, 570, GREEN)
    c.msg(A + 11, B - 11, 190, "GetDelayAsync()  (Stop awaits)", GREEN)
    bar(c, B, 190, 440, GREEN)
    c.msg(B - 11, A + 11, 290, "GetBoardAsync()  call back", GREEN, sw=4)
    c.text(640, 330, "accepted at the await", 28, BGREEN)
    bar(c, A, 290, 350, BGREEN, 36)
    c.msg(A + 11, B - 11, 400, "board", GREEN, "dashed")
    c.msg(B - 11, A + 11, 480, "delay", GREEN, "dashed")
    c.box(390, 520, 500, 80, "state can change between awaits", YEL, 24, dash="dashed")
    c.save("02-fix-reentrant.excalidraw")


# ---------------- 03 always interleave ----------------
def always_interleave():
    c = Canvas(); K, A, B = 150, 600, 1060
    lifeline_head(c, K, "Client", w=200); lifeline_head(c, A, "StopGrain", w=240); lifeline_head(c, B, "TripGrain", w=240)
    bar(c, A, 150, 615, GREEN)
    c.msg(A + 11, B - 11, 190, "GetDelayAsync()", GREEN)
    bar(c, B, 190, 400, GREEN)
    c.msg(K, A - 11, 250, "WatchAsync()", ORANGE)
    c.box(A - 280, 270, 230, 56, "waits in queue", ORANGE, 22, dash="dashed")
    c.msg(B - 11, A + 11, 340, "GetBoardAsync()", BGREEN, sw=4, dy=-24)
    bar(c, A + 30, 340, 380, BGREEN, 14) if False else None
    c.text(830, 390, "[AlwaysInterleave]\npasses the queue", 24, BGREEN)
    c.msg(A + 11, B - 11, 485, "board", GREEN, "dashed")
    c.msg(B - 11, A + 11, 545, "delay", GREEN, "dashed")
    c.msg(A - 11, K, 600, "Watch result (turn is done)", ORANGE, "dashed")
    c.text(380, 480, "only this method\npasses, others wait", 22, YEL)
    c.save("03-fix-always-interleave.excalidraw")


# ---------------- 04 oneway ----------------
def oneway():
    c = Canvas(); A, B = 330, 950
    lifeline_head(c, A, "StopGrain", color=GREY); lifeline_head(c, B, "TripGrain")
    bar(c, A, 150, 330, GREEN)
    c.msg(A + 11, B - 11, 220, "ReportAsync()  [OneWay]", GREEN, sw=4)
    c.text(640, 290, "no wait", 28, BGREEN)
    c.text(A + 150, 380, "turn ends,\nStop is free", 22, GREEN, "center")
    bar(c, B, 220, 470, GREEN)
    c.msg(B - 11, A + 11, 500, "no reply, no call back", DIM, "dashed")
    c.text(640, 560, "no wait, no cycle", 40, BGREEN)
    c.rect(400, 515, 480, 90, BGREEN, "transparent", 3)
    c.save("04-fix-oneway.excalidraw")


# ---------------- 05 cheat sheet ----------------
def cheatsheet():
    c = Canvas()
    W = 2300; cy = 440
    c.box(40, cy - 50, 200, 100, "Grain\nslow?", GREY, 28, kind="ellipse")
    qs = ["Read-heavy?", "Cycle between\ngrains?", "One key\ntoo hot?", "Must be\nglobal?"]
    ans = ["StatelessWorker\nor ReadOnly", "Reentrant,\nAlwaysInterleave\nor OneWay", "Split key\nor batch", "Keep single,\nadd cache"]
    xs = [440, 860, 1280, 1700]
    prev = 240
    for i, (q, a, x) in enumerate(zip(qs, ans, xs)):
        c.arrow([(prev, cy), (x - 130, cy)], GREY, 3)
        if i > 0:
            c.text((prev + x - 130) / 2, cy - 28, "no", 32, "#ffd43b")
        c.box(x - 130, cy - 100, 260, 200, q, PURPLE, 24, kind="diamond")
        c.arrow([(x, cy - 100), (x, 250)], GREY, 3)
        c.text(x + 40, 290, "yes", 32, BGREEN)
        c.box(x - 130, 100, 260, 150, a, GREEN, 24, bg="#12351f", tcolor="#d3f9d8")
        prev = x + 130
    c.arrow([(prev, cy), (2020, cy)], GREY, 3)
    c.text((prev + 2020) / 2, cy - 28, "no", 32, "#ffd43b")
    c.box(2020, cy - 70, 250, 140, "Profile the turn,\nmake it shorter", GREEN, 24, bg="#12351f", tcolor="#d3f9d8")
    c.text(1150, 640, "Grain slow? Follow the arrows.", 28, DIM)
    c.save("05-cheatsheet.excalidraw")


# ---------------- 06 readonly ----------------
def readonly():
    c = Canvas(); R1, R2, Wr, G = 130, 400, 670, 1050
    for x, l in [(R1, "Reader 1"), (R2, "Reader 2"), (Wr, "Writer")]:
        lifeline_head(c, x, l, w=210)
    lifeline_head(c, G, "Grain", w=220)
    c.msg(R1, G - 11, 170, "ReadOnly call", BLUE)
    c.msg(R2, G - 11, 240, "ReadOnly call", BLUE)
    bar(c, G, 170, 330, BLUE, 26)
    c.text(1170, 205, "reads\nrun together", 22, BLUE) if False else None
    c.text(G - 150, 300, "both reads interleave", 22, BLUE, "center") if False else None
    c.msg(Wr, G - 11, 350, "normal call (write)", ORANGE)
    c.box(Wr - 100, 375, 250, 56, "waits for reads", ORANGE, 22, dash="dashed")
    c.msg(G - 11, R1, 490, "reply 1", BLUE, "dashed")
    c.msg(G - 11, R2, 550, "reply 2", BLUE, "dashed")
    bar(c, G, 490, 625, ORANGE, 26)
    c.msg(G - 11, Wr, 610, "reply after the write turn", ORANGE, "dashed")
    c.text(1190 - 60, 200, "reads\ntogether", 22, BLUE, "center") if False else None
    c.save("06-readonly.excalidraw")


# ---------------- 07 stateless worker ----------------
def stateless():
    c = Canvas()
    c.text(120, 30, "Before", 32, RED)
    for i in range(4):
        y = 70 + i * 70
        c.box(40, y, 170, 52, "Phone " + str(i + 1) if i < 3 else "Phone ...", GREY, 22)
        c.arrow([(210, y + 26), (760, 150 + 70 + (i - 1.5) * 24)], RED, 3)
    c.box(760, 90, 340, 210, "StopGrain\nHauptbahnhof", RED, 30, sw=5)
    c.text(1180, 195, "one activation\ntakes all calls", 22, RED) if False else None
    c.line([(0, 345), (1280, 345)], DIM, 1, "dashed")
    c.text(100, 380, "After", 32, BGREEN)
    for i in range(4):
        y = 420 + i * 70
        c.box(40, y, 170, 52, "Phone " + str(i + 1) if i < 3 else "Phone ...", GREY, 22)
    for k, (sy, lab) in enumerate([(410, "Silo 1"), (580, "Silo 2")]):
        c.rect(400, sy, 340, 150, DIM, "transparent", 2, "dashed")
        c.text(470, sy + 22, lab, 20, DIM)
        c.box(430, sy + 40, 280, 90, "BoardCache\ncopy", GREEN, 22, bg="#12351f", tcolor="#d3f9d8")
        for p in (2 * k, 2 * k + 1):
            c.arrow([(210, 446 + p * 70), (430, sy + 85)], GREY, 2)
    for sy in (410, 580):
        c.arrow([(710, sy + 85), (890, 560)], GREEN, 2)
    c.text(960, 690, "every few seconds", 22, BGREEN) if False else None
    c.box(890, 500, 300, 120, "StopGrain\nHauptbahnhof", GREY, 26)
    c.text(1040, 680, "thin: one call every few seconds", 20, BGREEN)
    c.save("07-stateless-worker.excalidraw")


# ---------------- 08 hot grain ----------------
def hotgrain():
    c = Canvas()
    c.box(30, 270, 200, 100, "100 phones", GREY, 26)
    c.arrow([(230, 320), (330, 320)], RED, 4)
    c.box(330, 230, 280, 180, "StopGrain\nHauptbahnhof", RED, 28, sw=5)
    c.text(470, 450, "Silo 1: busy", 24, RED)
    c.box(330, 500, 200, 70, "Silo 2: idle", DIM, 22, dash="dashed")
    c.box(330, 590, 200, 70, "Silo 3: idle", DIM, 22, dash="dashed")
    c.text(470, 100, "Hot grain: one key, one activation", 28, RED)
    opts = [("Mostly reads?  Cache snapshot\n(StatelessWorker)", GREEN),
            ("Many small writes?  Batch:\napply once per second", GREEN),
            ("Slow calls?  Make them\n[OneWay]", GREEN),
            ("Key too coarse?  Split the key\n(per line or platform)", RED)]
    for i, (t, col) in enumerate(opts):
        y = 90 + i * 150
        c.box(810, y, 440, 110, t, col, 24, bg="#12351f" if col == GREEN else "#3a1220",
              tcolor="#d3f9d8" if col == GREEN else "#ffc9d4")
        c.arrow([(610, 320), (810, y + 55)], GREY, 2)
    c.save("08-hot-grain.excalidraw")


# ---------------- 09 source grain ----------------
def source():
    c = Canvas()
    names = ["StopGrain 1", "StopGrain 2", "StopGrain n", "AlertBoardGrain", "Reminders"]
    for i, n in enumerate(names):
        y = 120 + i * 110
        c.box(30, y, 260, 70, n, GREY, 24)
        c.arrow([(290, y + 35), (560, 350)], GREY, 2)
    c.box(560, 250, 340, 200, "SourceGrain\nONE, global", RED, 30, sw=5, bg="#3a1220")
    c.text(730, 480, "queue: one call at a time", 22, ORANGE)
    c.arrow([(900, 350), (1130, 350)], ORANGE, 4)
    c.text(1015, 318, "min 500 ms\nbetween calls", 22, ORANGE)
    c.box(1130, 290, 220, 120, "Linz AG\nEFA server", DIM, 24)
    # cache self-loop
    c.arrow([(660, 250), (660, 180), (800, 180), (800, 250)], BGREEN, 3, "dashed")
    c.text(730, 130, "cache per stop, 20 s\n+ stale fallback", 22, BGREEN)
    c.save("09-source-grain.excalidraw")


# ---------------- versioning helpers ----------------
V1 = "#e63946"; V2 = "#3a86ff"; V1BG = "#3a1220"; V2BG = "#12254a"


def silo(c, cx, cy, shape, color, filled=True, s=70):
    """Silo marker: shape = silo slot (square, circle, triangle), colour = grain version."""
    bg = color if filled else "transparent"
    if shape == "square":
        c.rect(cx - s / 2, cy - s / 2, s, s, color, bg, 3)
    elif shape == "circle":
        c.rect(cx - s / 2, cy - s / 2, s, s, color, bg, 3, kind="ellipse")
    else:
        pts = [(cx, cy - s / 2), (cx + s / 2, cy + s / 2), (cx - s / 2, cy + s / 2), (cx, cy - s / 2)]
        x0, y0 = pts[0]
        e = c._base("line", x0, y0, s, s, color, bg, 3)
        e.update({"points": [[px - x0, py - y0] for px, py in pts], "lastCommittedPoint": None,
                  "startBinding": None, "endBinding": None, "startArrowhead": None,
                  "endArrowhead": None, "polygon": True, "roundness": None})
        e.pop("lastCommittedPoint"); e.pop("startBinding"); e.pop("endBinding")


SHAPES = ["square", "circle", "triangle"]


# ---------------- 10 rolling update ----------------
def rolling_update():
    c = Canvas()
    X1 = [330, 450, 570]; X2 = [740, 860, 980]
    R = [100, 340, 580]
    for y, t in zip(R, ["1  only v1", "2  v2 joins", "3  v1 stopped"]):
        c.text(150, y, t, 30, GREY)
    c.arrow([(70, 145), (70, 290)], GREY, 3)
    c.text(90, 218, "start v2", 24, V2, "center") if False else c.text(175, 218, "start v2", 26, V2)
    c.arrow([(70, 385), (70, 530)], GREY, 3)
    c.text(175, 458, "stop v1", 26, V1)
    # stage 1
    for x, sh in zip(X1, SHAPES):
        silo(c, x, R[0], sh, V1)
    # stage 2: one cluster
    c.rect(280, 250, 780, 180, GREY, "transparent", 2, "dashed")
    c.text(670, 275, "one cluster", 24, GREY)
    for x, sh in zip(X1, SHAPES):
        silo(c, x, R[1] + 20, sh, V1)
    for x, sh in zip(X2, SHAPES):
        silo(c, x, R[1] + 20, sh, V2)
    c.text(670, 475, "BackwardCompatible + LatestVersion", 24, YEL)
    # stage 3: red crossed out, blue stays
    for x, sh in zip(X1, SHAPES):
        silo(c, x, R[2], sh, DIM, filled=False)
        c.line([(x - 32, R[2] - 32), (x + 32, R[2] + 32)], RED, 4)
        c.line([(x - 32, R[2] + 32), (x + 32, R[2] - 32)], RED, 4)
    for x, sh in zip(X2, SHAPES):
        silo(c, x, R[2], sh, V2)
    # legend
    c.rect(930, 30, 320, 140, DIM, "transparent", 2)
    silo(c, 975, 70, "square", V1, s=36); c.text(1020, 70, "v1", 24, V1, "center")
    silo(c, 1110, 70, "square", V2, s=36); c.text(1155, 70, "v2", 24, V2, "center")
    for x, sh in zip([975, 1050, 1125], SHAPES):
        silo(c, x, 125, sh, GREY, filled=False, s=36)
    c.text(1210, 135, "= silo", 22, GREY, "center") if False else c.text(1195, 125, "silo", 22, GREY)
    c.save("10-rolling-update.excalidraw")


# ---------------- 11 version compatibility ----------------
def version_compat():
    c = Canvas()
    c.box(80, 60, 300, 90, "v1 caller", V1, 30, bg=V1BG, tcolor="#ffc9d4")
    c.box(880, 60, 300, 90, "v2 grain", V2, 30, bg=V2BG, tcolor="#d0e4ff")
    c.arrow([(380, 105), (880, 105)], BGREEN, 5)
    c.text(630, 70, "BackwardCompatible", 28, BGREEN)
    c.text(630, 145, "works", 34, BGREEN)
    c.box(80, 260, 300, 90, "new activation", GREY, 30)
    c.box(880, 260, 300, 90, "v2 silo", V2, 30, bg=V2BG, tcolor="#d0e4ff")
    c.arrow([(380, 305), (880, 305)], BGREEN, 5)
    c.text(630, 270, "LatestVersion", 28, BGREEN)
    c.rect(80, 430, 1100, 200, ORANGE, "transparent", 3, "dashed")
    c.text(630, 530, "v2 only adds: a new method,\na new field with a new [Id]\nNever reuse or retype an [Id]", 38, YEL)
    c.save("11-version-compatibility.excalidraw")


if __name__ == "__main__":
    deadlock(); reentrant(); always_interleave(); oneway(); cheatsheet()
    readonly(); stateless(); hotgrain(); source()
    rolling_update(); version_compat()
