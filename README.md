# api_restatify-tensegrity-ai-cloud

**Cloud-Backend** von Tensegrity AI: hostet SLMs als Microservices für die WebAssembly-Variante der App und andere
Web-Clients. Zuerst antwortet ein **Generalist**, ein Router zieht bei Bedarf **Experten** hinzu.

## Komponenten

| Komponente | Technik |
|---|---|
| Gateway | ASP.NET Core, gRPC + gRPC-Web (ohne Envoy), Auslieferung der WASM-App |
| Identität | OIDC; Standard Keycloak (Social Login Google/Microsoft/Apple/Facebook), Auth0 per Konfiguration |
| Router | Auswahl Generalist/Experten |
| Inferenz | vLLM mit Multi-LoRA; Adapter aus Hive-Releases |
| Betrieb | Kubernetes, Helm |

Im Web sind die Stufen „Streng geheim“ und „Nur lokal“ gesperrt (ADR-0009).

## Dokumentation

Siehe [docs_restatify-tensegrity-ai](https://github.com/carryman1979/docs_restatify-tensegrity-ai).

## Lizenz

Apache License 2.0 – siehe [LICENSE](LICENSE).
