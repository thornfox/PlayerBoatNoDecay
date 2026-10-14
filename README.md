# Player Boat No Decay

**Developer:** ThornFox
**Version:** 1.1.0

[English](#english) | [Deutsch](#deutsch)

---

## English

Oxide/Carbon plugin for Rust that prevents player-built (modular) boats from taking decay damage.

### How it works

The plugin uses the `OnEntityTakeDamage` hook and discards any damage that consists entirely of `Decay` whenever the affected entity is a player boat or is attached to one (building blocks, sails, engines, deployables – detected via the parent chain).

Regular damage (cannons, rockets, explosive ammo, etc.) is not affected – boats can still be raided.

Not covered: boats drifting back to shore after being left at sea for a while. That is not damage but a separate game mechanic.

### Installation

1. Copy `PlayerBoatNoDecay.cs` into `oxide/plugins/` or `carbon/plugins/`.
2. The plugin loads automatically and protects all player boats by default.

### Configuration

File: `oxide/config/PlayerBoatNoDecay.json` (or the Carbon equivalent)

```json
{
  "Only protect boats of players with permission (false = protect all player boats)": false,
  "Protected entity types (class name or short prefab name)": [
    "PlayerBoat"
  ],
  "Debug output in console": false
}
```

| Option | Description |
|---|---|
| Only protect boats of players with permission | `true` = only boats whose owner has the permission are protected; `false` = all player boats are protected |
| Protected entity types | Class or prefab names that count as a player boat. Can be adjusted if Facepunch renames them |
| Debug output in console | Logs every blocked decay tick to the console (for testing) |

### Permission

| Permission | Effect |
|---|---|
| `playerboatnodecay.use` | Boats owned by this player do not decay (only relevant when the permission option is enabled) |

Example: `oxide.grant group vip playerboatnodecay.use`

### Updating from 1.0.x

Version 1.1.0 switched the configuration keys from German to English. Existing config files are rewritten automatically with the new keys and **default values**. If you changed any settings, re-apply them after the update.

### Alternative without a plugin

Set the server ConVars `playerboat.decayminutes` and `playerboat.decaystartdelayminutes` to very high values.

---

## Deutsch

Oxide/Carbon-Plugin für Rust, das verhindert, dass selbstgebaute (modulare) Spieler-Boote durch Decay Schaden nehmen.

### Funktionsweise

Das Plugin nutzt den Hook `OnEntityTakeDamage` und verwirft jeden Schaden, der vollständig aus `Decay` besteht, sobald das betroffene Entity ein Spieler-Boot ist oder daran hängt (Bauteile, Segel, Motoren, Deployables – erkannt über die Eltern-Kette).

Normaler Schaden (Kanonen, Raketen, Explosivmunition usw.) bleibt unverändert – Boote sind weiterhin raidbar.

Nicht abgedeckt: das Zurücktreiben zur Küste, wenn ein Boot länger auf See liegt. Das ist kein Schaden, sondern eine eigene Spielmechanik.

### Installation

1. `PlayerBoatNoDecay.cs` nach `oxide/plugins/` bzw. `carbon/plugins/` kopieren.
2. Das Plugin lädt automatisch und schützt standardmäßig alle Spieler-Boote.

### Konfiguration

Datei: `oxide/config/PlayerBoatNoDecay.json` (bzw. Carbon-Äquivalent)

```json
{
  "Only protect boats of players with permission (false = protect all player boats)": false,
  "Protected entity types (class name or short prefab name)": [
    "PlayerBoat"
  ],
  "Debug output in console": false
}
```

Die Schlüssel der Konfiguration sind auf Englisch. Ihre Bedeutung:

| Option | Beschreibung |
|---|---|
| Only protect boats of players with permission | Nur Boote von Spielern mit Permission schützen. `true` = nur Boote von Besitzern mit Permission werden geschützt; `false` = alle Spieler-Boote werden geschützt |
| Protected entity types | Geschützte Entity-Typen: Klassen- oder Prefab-Namen, die als Spieler-Boot gelten. Anpassbar, falls Facepunch Namen ändert |
| Debug output in console | Debug-Ausgabe: meldet in der Konsole jeden blockierten Decay-Tick (zum Testen) |

### Permission

| Permission | Wirkung |
|---|---|
| `playerboatnodecay.use` | Boote dieses Besitzers decayen nicht (nur relevant, wenn die Permission-Option aktiv ist) |

Beispiel: `oxide.grant group vip playerboatnodecay.use`

### Update von 1.0.x

Mit Version 1.1.0 wurden die Konfigurationsschlüssel von Deutsch auf Englisch umgestellt. Bestehende Konfigurationsdateien werden automatisch mit den neuen Schlüsseln und **Standardwerten** neu geschrieben. Falls du Einstellungen geändert hattest, trage sie nach dem Update erneut ein.

### Alternative ohne Plugin

Die Server-ConVars `playerboat.decayminutes` und `playerboat.decaystartdelayminutes` auf sehr hohe Werte setzen.
