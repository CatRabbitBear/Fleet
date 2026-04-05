The project is a proof of concept, a program running in the system tray capable of running agents / LLM workflows.

There may be some permissions challenges, i.e windows tasks from Fleet.Blazor or web API tasks from Fleet.Tray.

An SQLite DB should be used for 
- storing agent configurations like models, prompts, tools and other resources
- storing artifacts from agents runs
- Any other necessary persistence

There is a prototype browser extension that has been shown to be able to communicate and execute tasks, including adding context from
the current webpage.

Some other use cases for a system like this could be
- "The user activates the browser extension and chooses to add context from the webpage and some instuctions, the results of the pipeline
   should be stored in the BD and accessible using a simple CRUD page using Fleet.Blazor"
- "The user right-clicks on the tray icon and can hover overs an 'agents' or 'workflows' dropdown menu to select pre-configured tasks
   like checking email, checking news from a websearch / RSS feed, making amendments to a document in the local filesystem"
- "The user can use Fleet.Blazor's web app to configure agents and tasks, including models from Azure, prompts to use, pre-configured pipelines to include
   permissions, and where to store results"