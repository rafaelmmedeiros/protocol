# M3 — progress

**Status:** in progress

One entry per step of `plan.md`, in the plan's linearised order. Git carries what changed; this
file carries what a future session would otherwise rediscover.

### S3.1 — The Hevy connection
- **Status:** completed
- **Tests:** 4 unit (`HevyKeyProtectionTests`), 9 integration (`HevyConnectionEndpointsTests`)
- **Observations:**
  - **`IHevyClient` had to be born in this step, not `S3.2`.** The plan lists `S3.1` as depending
    on nothing, but validating a key before storing it (`ADR-014`) needs a call to Hevy. The seam
    was introduced here holding only `CheckKeyAsync`, and `S3.2` grows it. The dependency order in
    the plan still holds; what was wrong was the implication that `S3.1` touches no client.
  - **The restart trap is testable in-process and worth the machinery.** `ADR-014` names an
    ephemeral key ring as the failure mode, and a second `WebApplicationFactory` pointed at the
    same Testcontainers database *is* a restart. `A_stored_key_still_decrypts_after_the_host_restarts`
    fails loudly without `PersistKeysToDbContext`. This was cheaper than waiting for rung 7 to
    catch it in `S3.8`.
  - **`SetApplicationName` is not optional and would have been missed.** Data Protection otherwise
    derives the name from the content root, which differs between the container and a test host —
    so a ring correctly persisted to the database would still fail to decrypt under a different
    name. The restart test caught this shape of problem, not the discipline.
  - **The key ring lives in the database rather than on a volume.** Chosen so a restored backup
    brings its own keys: a filesystem ring and a database can drift apart, and the only symptom is
    that nothing decrypts. This is a decision `ADR-014` left open, recorded in the context and in
    `backend/CLAUDE.md` rather than as a new record.
  - **`ApiFactory` now substitutes Hevy for the whole suite.** That makes `S3.8`'s "no test run
    touches the real Hevy account" true by construction instead of by everyone remembering.
  - **Two plan corrections, made rather than deferred.** The connection is its own table instead of
    two columns on the user, and the key is text instead of bytes. Both are recorded in the plan's
    Specifications with the reasons.
  - **Not built, and deliberately: there is no disconnect.** The capability is "connect a Hevy
    account", `DELETE /hevy/connection` is outside the plan's technical actions, and adding it here
    would be scope this step was not given. Reported rather than fixed.

### S3.2 — The Hevy boundary
- **Status:** pending

### S3.3 — Pushing a week
- **Status:** pending

### S3.4 — Importing history
- **Status:** pending

### S3.5 — Prescribed against performed
- **Status:** pending

### S3.6 — Equipment the history reveals
- **Status:** pending

### S3.7 — The screens
- **Status:** pending

### S3.8 — The ladder, containerized
- **Status:** pending
