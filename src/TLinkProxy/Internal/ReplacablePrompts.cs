namespace TLinkProxy.Internal
{
#pragma warning disable CC0009
    public static class ReplacablePrompts
    {
        public static readonly (string Find, string Replace)[] Rules =
        [
            ("You are an AI programming assistant.", ""),
            ("Follow Microsoft content policies.", ""),
            ("Avoid content that violates copyrights.", ""),
            ("Keep your answers short and impersonal.", ""),
            ("Respond in the following locale: en-US", ""),
            ("You are a highly sophisticated automated coding agent with expert-level knowledge across many different programming languages and frameworks.", ""),
            ("Users workspace may be an open source repository but your goal is still to implement the fix in their workspace directory. You should not assume their files are same as the open source repository.", ""),
            ("Think step-by-step:", ""),
            ("1. Analyze what information the user has already provided", ""),
            ("2. Determine what additional context you need", ""),
            ("3. Choose the most efficient tools to gather that context", ""),
            ("4. Implement the solution", ""),
            ("Choose tools strategically based on your information needs:", ""),
            ("PLANNING GATE", ""),
            ("IF A PLAN ALREADY EXISTS", ""),
            ("CREATING A NEW PLAN", ""),
            ("FOLLOWING PLAN DETAILS", ""),
            ("HANDLING ISSUES", ""),
            ("TERMINAL GUIDANCE", ""),
            ("Examples:", ""),
            ("You can use any tool multiple times in a response, and you can keep writing text after using a tool.", ""),
            ("- Use existing libraries whenever possible and only add new libraries or update library versions if absolutely necessary.", ""),
            (", so you can make changes properly", ""),
            (", and you can keep writing text after using a tool", ""),
            ("- Never say the name of a tool to a user. Never ask permission to use a tool.", ""),
            (" (backend + frontend, API + tests, code + config)", "),")
        ];

        public static readonly (string startBlock, string endBlock)[] RemoveableBlocks =
        [
            ("<testing_guidance>", "</testing_guidance>"),
            ("<maximize_context_understanding>", "</maximize_context_understanding>"),
            ("<context_gathering_strategy>", "</context_gathering_strategy>"),
            ("<tool_guidance>", "</tool_guidance>"),
        ];

        public static readonly string[] Tools = [
            "start_modernization", "profiler_agent", "query_azure_resource_graph", "code_search", "get_errors", "file_search", "get_files_in_project",
            "get_projects_in_solution", "run_build remove_file", "create_file", "run_command_in_terminal", "get_background_terminal_output",
            "search_agent", "plan update_plan_progress", "finish_plan record_observation", "adapt_plan",
            "signal_plan_ready", "clarify_requirements", "ask_question", "detect_memories", "get_output_window_logs",
            "run_tests get_tests", "find_symbol", "get_web_pages", "grep_search get_file", "replace_string_in_file",
            "multi_replace_string_in_file", "task_complete"];
    }
#pragma warning restore CC0009
}
