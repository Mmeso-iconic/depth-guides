"""
Depth Guides: pilot analysis

Summarises the results CSV and (if matplotlib is installed) draws a chart.

Usage:
    python analysis/analyze.py data/pilot_results.csv

The CSV has one row per placement, with the columns written by the game:
name, player_id, 3d_experience, order, trial, guides, target_depth,
depth_error, side_error, height_error, total_error, time_s, easier_round
Results from phones are typed in from the screenshots.
"""

import csv
import sys
from statistics import mean

MEASURES = ["depth_error", "side_error", "height_error", "total_error", "time_s"]


def load(path):
    with open(path, newline="", encoding="utf-8") as f:
        rows = list(csv.DictReader(f))
    for r in rows:
        for m in MEASURES + ["target_depth"]:
            r[m] = float(r[m])
    return rows


def avg(rows, measure):
    values = [r[measure] for r in rows]
    return mean(values) if values else float("nan")


def table(rows, title):
    print(f"\n{title}")
    print(f"{'':<12}" + "".join(f"{m:>14}" for m in MEASURES))
    for g in ["OFF", "ON"]:
        sub = [r for r in rows if r["guides"] == g]
        print(f"{'Guides ' + g:<12}" + "".join(f"{avg(sub, m):>14.2f}" for m in MEASURES))


def main(path):
    rows = load(path)
    people = sorted({r["player_id"] for r in rows})
    print(f"Participants: {len(people)}   Placements: {len(rows)}")

    # H1 and H2: where do people go wrong, and do guides help?
    table(rows, "ALL PARTICIPANTS (average per placement)")

    # H3: do guides help beginners more?
    for exp in ["no", "yes"]:
        sub = [r for r in rows if r["3d_experience"] == exp]
        if sub:
            n = len({r["player_id"] for r in sub})
            table(sub, f"3D EXPERIENCE = {exp.upper()} ({n} people)")

    # H4: are further targets harder? Compare the nearer and further half of targets
    depths = sorted(r["target_depth"] for r in rows)
    middle = depths[len(depths) // 2]
    near = [r for r in rows if r["target_depth"] < middle]
    far = [r for r in rows if r["target_depth"] >= middle]
    print("\nNEAR vs FAR TARGETS (depth error)")
    print(f"Nearer targets: {avg(near, 'depth_error'):.2f}   Further targets: {avg(far, 'depth_error'):.2f}")

    # What people felt
    feelings = {}
    for pid in people:
        answer = next(r["easier_round"] for r in rows if r["player_id"] == pid)
        feelings[answer] = feelings.get(answer, 0) + 1
    print("\nWHICH ROUND FELT EASIER?")
    for k, v in feelings.items():
        print(f"{k}: {v}")

    chart(rows)


def chart(rows):
    try:
        import matplotlib.pyplot as plt
    except ImportError:
        print("\n(Install matplotlib to also get a chart: pip install matplotlib)")
        return
    labels = ["Depth", "Side", "Height"]
    keys = ["depth_error", "side_error", "height_error"]
    off = [avg([r for r in rows if r["guides"] == "OFF"], k) for k in keys]
    on = [avg([r for r in rows if r["guides"] == "ON"], k) for k in keys]
    x = range(len(labels))
    plt.figure(figsize=(6, 4))
    plt.bar([i - 0.2 for i in x], off, width=0.4, label="Guides OFF", color="#9a9aa3")
    plt.bar([i + 0.2 for i in x], on, width=0.4, label="Guides ON", color="#FF6B1A")
    plt.xticks(list(x), labels)
    plt.ylabel("Average error (units)")
    plt.title("Where placement errors happen")
    plt.legend()
    plt.tight_layout()
    plt.savefig("analysis/error_chart.png", dpi=150)
    print("\nChart saved to analysis/error_chart.png")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print(__doc__)
    else:
        main(sys.argv[1])
