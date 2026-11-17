#!/usr/bin/env bash
# Records the EFA answers that ReplayFeed serves: one departure monitor file per stop of the
# catalogue plus the service notices. Raw XML, stored as the server sent it.
#
# Run it by hand, ideally during the day so the boards show realtime data:
#   ./record.sh
# One request at a time with a pause in between, to stay polite towards Linz AG.
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
catalogue="$here/../../../Stops/GleisGrain.Stops/Catalogue/stops.json"
base="https://www.linzag.at/static"
agent="gleis-grain-demo"
pause=1

mkdir -p "$here/departures"

fetch() {
  curl --silent --show-error --fail --max-time 30 --user-agent "$agent" --output "$2" "$1"
  sleep "$pause"
}

for stop in $(grep -o '"id": *"[^"]*"' "$catalogue" | grep -o '[0-9]\+'); do
  echo "departures $stop"
  fetch "$base/XML_DM_REQUEST?outputFormat=XML&type_dm=stop&name_dm=$stop&mode=direct&useRealtime=1&limit=20" \
    "$here/departures/$stop.xml"
done

echo "notices"
fetch "$base/XML_TRIP_REQUEST2" "$here/notices.xml"
