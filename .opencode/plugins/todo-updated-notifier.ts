import type { Plugin } from "@opencode-ai/plugin"

export const TodoUpdatedNotifier : Plugin = async ({ $ }) => {
  return {
    event: async ({ event }) => {
      if (event.type === "todo.updated") {
        await $`notify-send 'Attencion' 'TODO updated'`
      }
    },
  }
}
