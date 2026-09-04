# Concertable.Auth.StartupTests — startup tests

**Would either Auth host refuse to boot, given the configuration its app model supplies it?** These build
the real production registration graph, and the AppHost's resource graph, without starting either or
reaching external infrastructure. Tests that execute requests, business operations or infrastructure belong
in integration or E2E projects; tests that assert code structure belong in an architecture project.

Host coverage and activation rules: the `composition-testing` skill.
