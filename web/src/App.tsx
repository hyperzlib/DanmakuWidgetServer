import type { Component } from "solid-js";
import { createResource, createSignal, For, Show } from "solid-js";
import { fetchTemplates } from "./api/templates";
import TemplateCard from "./components/TemplateCard";

// ---- App ----
const App: Component = () => {
  const [data] = createResource(fetchTemplates);
  const [selectedBase, setSelectedBase] = createSignal("");

  const baseUrl = () => {
    const d = data();
    if (selectedBase()) return selectedBase();
    if (d && d.base_url_list.length > 0) return d.base_url_list[0];
    return "";
  };

  return (
    <div class="min-h-screen text-gray-900 dark:text-gray-100 bg-gray-100 dark:bg-gray-900">
      {/* Header */}
      <header class="bg-white/75 backdrop-blur border-b border-gray-100 sticky top-0 z-10 shadow-sm dark:bg-black/75 dark:border-gray-800">
        <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between gap-4">
          <div>
            <h1 class="text-lg font-bold">
              <span class="text-gray-900 leading-tight dark:text-gray-100">
                Web 弹幕机
              </span>
              <span class="text-gray-500 text-sm ml-2 leading-tight dark:text-gray-400">
                From 弹幕姬
              </span>
            </h1>
            <p class="text-xs text-gray-500 leading-tight dark:text-gray-400">
              模板列表
            </p>
          </div>
          <Show when={data() && !data.loading}>
            <div class="flex items-center gap-2">
              <label
                for="base-url-select"
                class="text-sm text-gray-500 whitespace-nowrap hidden sm:block dark:text-gray-400"
              >
                服务地址
              </label>
              <select
                id="base-url-select"
                class="text-sm border border-gray-200 rounded-lg px-3 py-1.5 bg-transparent text-gray-700 focus:outline-none focus:ring-2 focus:ring-indigo-400 max-w-xs dark:border-gray-700 dark:text-gray-200"
                value={baseUrl()}
                onChange={(e) => setSelectedBase(e.currentTarget.value)}
              >
                <For each={data()!.base_url_list}>
                  {(url) => <option value={url}>{url}</option>}
                </For>
              </select>
            </div>
          </Show>
        </div>
      </header>

      {/* Main */}
      <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Loading */}
        <Show when={data.loading}>
          <div class="flex items-center justify-center py-32 gap-3 text-gray-400 dark:text-gray-500">
            <svg
              class="animate-spin w-5 h-5"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
            >
              <circle
                class="opacity-25"
                cx="12"
                cy="12"
                r="10"
                stroke="currentColor"
                stroke-width="4"
              />
              <path
                class="opacity-75"
                fill="currentColor"
                d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"
              />
            </svg>
            <span class="text-sm">加载中...</span>
          </div>
        </Show>

        {/* Error */}
        <Show when={data.error}>
          <div class="flex flex-col items-center justify-center py-32 gap-2 text-red-500 dark:text-red-400">
            <svg
              xmlns="http://www.w3.org/2000/svg"
              class="w-8 h-8"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="1.5"
                d="M12 9v2m0 4h.01M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"
              />
            </svg>
            <p class="text-sm">
              加载失败：{String(data.error?.message ?? data.error)}
            </p>
          </div>
        </Show>

        {/* Template Grid */}
        <Show when={data() && !data.loading && !data.error}>
          <Show
            when={(data()!.templates?.length ?? 0) > 0}
            fallback={
              <div class="text-center py-32 text-gray-400 dark:text-gray-500 text-sm">
                暂无可用模板
              </div>
            }
          >
            <div class="flex flex-col gap-4">
              <For each={data()!.templates}>
                {(template) => (
                  <TemplateCard template={template} baseUrl={baseUrl()} />
                )}
              </For>
            </div>
          </Show>
        </Show>
      </main>
    </div>
  );
};

export default App;
