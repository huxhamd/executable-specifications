# NOTES

INTRO / CAVEAT
* Not my Ideas. Dave Farley - Literally wrote the originl book on Continuous Deployment.
* Continuous Delivery: Reliable Software Releases through Build, Test, and Deployment Automation (2010), co-written with Jez Humble. It won the 2011 Jolt Excellence Award and is widely seen as the defining book on the subject.
* Not just Dave - Many leaders and Orgs within the industry are converging on some common themes / ideas - Are they on to something?

THE PROBLEM
* The amount of code being written is growing exponentially, but fewer-and-fewer devs are reading it.
* Including coding veterans like Uncle Bob Martin (big voice in the industry and Accedited with co-authoring the Agile Manifesto and SOLID principles)
* Not laziness - by design.

* Constrain the agent with Unit tests, gherkin tests, QA procedures, quality metrics, mutation testing, test coverage. We are still managing a lot of this stuff manually.
* AI first development process - Allow us to move up an abstraction layer. Concentrate on building systems, rather than features, or even pulling together parts of features.
* When the machine writes the code, the humans job is to say PRECISELY what the code should do. (in a form that can be checked automatically)

* Figure out what it's meant to do + Check it does it properly = The hard bit.
* AI does not change: 1. Specify. 2. Verify. 3. Small safe steps.
* What AI does change: Implementation is now easy, fast, and cheap.

* End2End tests, BDD, Spec Driven Development - All good, but individually none go far enough
* TDD, unit tests, and integration tests are all best practices.
* We need to feed the spec to the model and test against the spec.
* How do we pull all this together?

* Make the tests the specification
* Prompts are too vague. Open to interpretation. No where near as precise as a program.
* Agents are non-deterministic. The can under deliver, over deliver, deliver something adjacent to what is needed.
* Humans can't keep up. How can we be certain that existing behaviour has not regressed or drifted after each round of changes?
* Agents can produce thousands of lines a minute - encourages us to take steps that are too large, which increases risk.
* No human can thoroughly review the amount of code that is being generated in a meaningful way. Details get lost in the noise.

* How fast can we generate code? = Wrong question.
* How fast can we know the code we generated is doing the right thing? = Better question.
* Anything that's not well tested becomes stochastically degraded.
* Lets scale up by getting the agent to write all the tests! = BAD IDEA
* Lets get the agent to back-write all the missing tests! = BAD IDEA
* Tests generated from code only confirm the code is doing what the code was doing, it can't tell us if the code is doing what it was intened to do.
* Intent has to come from us.

* Industry responded with Spec Driven Developement. GitHub Spec Kit. Amazon Kiro. Tessl. Open Spec. BMAD. Get Shit Done. Jama Connect.
* Dignosis is correct, but the implementation misses the point. (Prose are too open to interpretation)
* Can't steer an agent with vibes - too vague.
* Need a clear statement before the agent starts (to  work from). Need the clear statement to survive after the agent has finished (for verification).
* ...much better than "Goal: ...", then manually review the large block of code it hands you back.
* But the industry SDD solutions, the specs are still prose.
* Common complaint that the prose lack detail and the real spec becomes the unit and integration tests.

* "Code is law because its a formal language you can reason about. You can prove it is right or wrong. Specs written in prose lack this formality." - Colin EberHardt (FINOS Open Source Foundation - Part of Linux). That's the whole problem in one paragraph.
* "BDD done well collapses spec writing and spec validation into the same artifact." - Markus Eisele (AI Development Strategist at IBM). This is exactly the point.
* Specification and validation should be the same thing. When they are the spec cant drift from code because the build breaks.
* Executable specs / Acceptance Test Driven Development are examples of this in action.
* Written in the language of the problem domain, they run against the real system and fail until the behaviour they describe exists.

* Spec vs Design.
* A spec is - "GIVEN a customer with a book in their basket, WHEN they apply the discount code SUMMER, THEN the basket total is reduced by 10%".
* The design is, "Is there a button?", "How big / What colour is it?", "Where is the data stored?" , etc...
* Designs break the moment someone finds a better one.
* The spec will survive design changes and preseve the solution intent.

* Acceptance Test Driven Development
* In practice - Four layer architecture:
  1. Executable Test cases - Short scenarios written in the language of the problem domain. E.g.
      `[Fact] UserCanAddATodo() { await Todos.Add("Buy milk"); await Todos.ConfirmShown("Buy milk"); }`
  2. Domain Specific Language (DSL) - Prepares and judges. Prepares / validates / Adds defaults / Test Isolation / Calls the driver / Asserts if the test passes or fails. Methods are named using the vocabulary of the domain so that specs are easy to read - Hence "Domain Specific Language" or DSL for short. Makes tests easy to write and turns behaviours into consistent reusable operations.
      `Add(string title) { Driver.Add(Alias(title)); Assert.False(result.Failed); }`
  3. Protocol Driver - Knows how to stimulate the app via its different channels. Knows how to talk to the real system. E.g.
      API - `Add(string title) { http.PostAsJson("/api/todos", title) }`
      UI - `Add(string title) { page.GotoAsync("/"); page.getByTestId("add-todo").Click() }`.
  4. System Under Test (SUT) - The app.
* Spec lives at the top. All the technical couplign lives at the bottom.
* Change the whole implementation / swap the web framework / rewrite it in another language - The spec still holds!

* This approach works very well for AI Agents.
* Relative freedom over how it implements things.
* No freedom at all over what the system is meant to do.
* The spec is its fitness function - Must be satisfied.

* S/W developement = a translation
* Vague Wish > Story > Example > EXECUTABLE SPECIFICATION > Code & Tests > Working Software
* Human owns the first four. Agent owns the last two.
* Always had to do these steps, the shift is only in emphasis.

* RULES
  - Write the spec before the Agent starts (one scenario ata time).
  - Watch it fail. RED first is not ceremony, it is the proof your spec will catch a wrong answer.
  - Keep the spec out of the agents reach, so that it can't edit or delete the test to make it pass.
  - Work in small steps. Agile.
  - Acceptance tests ARE the contract. A human MUST approve.

* Work in small steps. One scenario, red, green, review, commit.
* Agents don't need small step, but humans do!
* We need to find out if the output was wrong when it is still cheap to be wrong. If an Agent outputs 3000 lines of code it is a large step and difficult to review and know if it was good step or a stumble.
* Need more robust verification than staring at thousands of lines of code.

* Critisism - Water fall in a trenchcoat? Only if you spec the whole system up front.
* Designed to be incremental (Agile).
* Should we read the code? Opinons in the wild vary. I think in our environment we should, but we should have an EXCEPTIONALLY high confidence before it gets in front of a human.
* Executable specs do not stop us from readin the code. It becomes a choice we may take to mitigate risk, rather than an obligation.
* This is the difference between a practice that scales to meet the rate of AI code generation, and one that doesn't.

* Critisism - Acceptance tests are slow, brittle, and assertion light.
* Badly built acceptance tests can be those things.
* "Specs are often not very assertion heavy, giving us a false sense of security." - Martin Fowler (Agile Manifesto)
* Tests coupled to the UI break when the UI changes - Answer: don't couple it to the UI then. (Update the Protocol Driver to suit)

* Mutation testing to find holes in your tests. Then plug them!
* AI can help write the specs, but nothing counts as a spec until reviewed by a human
* AI can help wite the implementation (4 layer model)
* AI implements the code to make tests pass.
* Tests run in the deployment pipeline.
* Tests run on every commit (CD style) - Outsie of AIs control in order to verify results.

* Spec Driven Developement + Executable Specs = Acceptance Test Driven Development
* Four layer model / Language of the problem domain / red before green / separation of what from how - None of it new + written down + proven to work at scale decades ago.
* Farley's course is £599.
* I've figured it out for free.