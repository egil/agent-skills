# Marker schema

`emit-marker.ps1` writes exactly one line to stdout:

`CODEX_DELIVERY_MARKER:{"schemaVersion":1,...}`

It never writes a file. The marker payload is deliberately smaller than native Codex telemetry.

| Field | Required | Meaning |
| --- | --- | --- |
| `schemaVersion` | yes | Always `1`. |
| `runId`, `workItem`, `role` | first marker | Stable delivery context. Later markers inherit it. |
| `phase` | no | A material delivery phase. |
| `result` | no | `succeeded`, `failed`, `blocked`, `incomplete`, or `not-run`. |
| `workCycle` | no | Boolean marking one material hypothesis/change/validation cycle. |
| `qualityOutcome`, `findingCount`, `resolvedFindingCount` | no | A material quality result and exact supplied counts. |
| `blocker`, `outcome` | no | Bounded semantic blocker or terminal outcome. |
| `routingClass`, `routingRationale` | no | Selected routing category and short, non-sensitive rationale. |

The extractor recognizes only markers in tool-output strings containing the exact prefix. It supports the observed direct string and array/text wrappers recursively. Context conflicts reject the whole session rather than guessing.
