# CLAUDE.md – api_restatify-tensegrity-ai-cloud

C#-Gateway + vLLM-Deployment. Fachliche Referenz: Docs-Repo (ADR-0008/9, Ökosystem Abschnitt 4.3).

## Regeln
- Anfragen mit Stufe Streng geheim oder Nur lokal ablehnen (serverseitig, nicht nur in der UI).
- Keine Prompts/Antworten persistent speichern oder loggen; Feedback nur nach Freigabe an den Hive.
- OIDC über Standardbibliotheken; keine IdP-spezifischen SDKs im Anwendungscode.
- Adapter nur aus TUF-geprüften Hive-Releases laden.
- Tests: MSTest (Unit/Komponente), Testcontainers mit Keycloak (Integration), Lasttests vor Release.
- Kein `git push` ohne Freigabe.
