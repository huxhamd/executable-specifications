# OBSERVATIONS

* Isolation comes from Aliasing not from reseting the database.

* Assert on what the spec created, never on global state.

* Boundaries arrive when the app needs them, not because a spec does. E.g. It is okay that the "Clear Todos" button clears all Todos for all users because the app has no concept of Users yet. Once users exist for real product reasons, functional isolation uses them naturally, and specs like “a new user sees an empty list” become valid.

* Spec vs Design. Spec is "GIVEN a customer with a book in their basket, WHEN they apply the discount code SUMMER, THEN the basket total is reduced by 10%". Design is, "Is there a button?" and "How big / What colour is it?"

* Solves the "What tests run in SANDBOX vs STAGING?" problem.

* Tests kept out of agents reach to stop them deleting tests, rather than fixing them.

* Work in small step - not because the agent can't handle it (it can), but because a person / people can't review at scale.

* Not waterfall - The spec is built and implemented incrementally just like Agile.

* Acceptance tests are decoupled from the UI / API / etc. If the UI changes, then update the driver. Even better update the driver + contract + the design references and get the agent to implement the UI to suit.

* Spec Driven Developement + Executable Specs = Acceptance Test Driven Development