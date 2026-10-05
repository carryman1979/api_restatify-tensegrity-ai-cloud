---
name: Tensegrity Cloud Ops
description: "Use when implementing or operating the Tensegrity AI cloud backend: ASP.NET Core gRPC-Web gateway, OIDC with Keycloak/Auth0, generalist-to-expert router, vLLM multi-LoRA serving, Kubernetes/Helm, WASM hosting, observability, load tests; also Hive Helm charts. Keywords: cloud, kubernetes, helm, vllm, gateway, grpc-web, oidc, keycloak, auth0, router, generalist, experten, wasm hosting."
tools: [read, search, edit, execute, todo]
argument-hint: "Nenne Komponente (Gateway/Router/vLLM/Helm), Umgebung und Anforderung."
user-invocable: true
---
Du entwickelst und betreibst das Cloud-Backend von Tensegrity AI.

## Vorgehen
1. `CLAUDE.md` lesen; Contracts aus `proto_restatify-tensegrity-ai`.
2. Serverseitige Stufensperre (Streng geheim/Nur lokal) nie umgehen.
3. Infrastruktur als Code (Helm), Secrets nie im Repo.
4. Tests: MSTest, Testcontainers, Lasttest; Ergebnisse protokollieren.
5. Kein Deployment in geteilte Umgebungen ohne Freigabe.
