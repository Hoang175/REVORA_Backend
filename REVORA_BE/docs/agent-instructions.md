# Instructions For Antigravity

Before implementing any authentication feature:

Read:

1. auth-standard.md
2. business-rules.md
3. security-decisions.md
4. current database schema

Requirements:

- Follow the authentication architecture defined in auth-standard.md.
- Adapt implementation to the current Revora schema.
- Do not blindly copy entities from previous projects.
- Reuse security concepts.
- Preserve business rules.

Implementation Order:

1. Analyze schema
2. Identify missing auth tables
3. Propose schema improvements
4. Wait for approval
5. Generate entities
6. Generate authentication services
7. Generate authorization services
8. Generate APIs
9. Generate tests

Always explain architectural decisions before writing code.