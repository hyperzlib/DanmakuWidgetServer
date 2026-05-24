import type { Component } from 'solid-js';
import { Show } from 'solid-js';
import type { TemplateManifest } from '../types/api';
import CopyButton from './CopyButton';

const TemplateCard: Component<{ template: TemplateManifest; baseUrl: string }> = (props) => {
  const templateUrl = () => props.baseUrl + props.template.template_url;
  const configureUrl = () =>
    props.template.configure_url ? props.baseUrl + props.template.configure_url : null;
  const previewImg = () =>
    props.template.preview_img ? props.baseUrl + props.template.preview_img : null;

  return (
    <div class="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden dark:bg-gray-900 dark:border-gray-800">
      <div class="flex flex-col sm:flex-row lg:grid lg:grid-cols-[280px_minmax(0,1fr)_340px]">
        {/* Left: Thumbnail */}
        <div class="relative h-48 sm:h-auto sm:w-64 lg:w-auto lg:h-full min-h-48 lg:min-h-32 bg-gray-100 overflow-hidden dark:bg-gray-800 shrink-0">
          <Show
            when={previewImg()}
            fallback={
              <div class="h-full w-full flex flex-col items-center justify-center gap-2 text-gray-300 dark:text-gray-600">
                <svg xmlns="http://www.w3.org/2000/svg" class="w-10 h-10" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                </svg>
                <span class="text-xs">暂无预览图</span>
              </div>
            }
          >
            <>
              <img
                src={previewImg()!}
                alt={`${props.template.name} 背景`}
                class="absolute inset-0 h-full w-full object-cover scale-110 blur-xl opacity-60"
              />
              <div class="absolute inset-0 bg-black/10 dark:bg-black/30" />
              <img
                src={previewImg()!}
                alt={`${props.template.name} 预览图`}
                class="relative z-10 h-full w-full object-contain"
              />
            </>
          </Show>
        </div>

        <div class="flex-1 min-w-0 flex flex-col sm:border-l sm:border-gray-100 dark:sm:border-gray-800 lg:contents">
        {/* Middle: Basic Info */}
        <div class="p-5 flex flex-col gap-3 min-w-0">
          <div>
            <div class="flex items-center gap-2 flex-wrap mb-1">
              <h2 class="font-semibold text-gray-900 text-base dark:text-gray-100">{props.template.name}</h2>
              <Show when={props.template.version}>
                <span class="text-xs bg-blue-50 text-blue-600 px-1.5 py-0.5 rounded font-mono border border-blue-100 dark:bg-blue-900/40 dark:text-blue-300 dark:border-blue-800">
                  v{props.template.version}
                </span>
              </Show>
            </div>
            <p class="text-sm text-gray-500 leading-relaxed dark:text-gray-400">{props.template.description}</p>
          </div>

          <Show when={props.template.author}>
            <div class="flex items-center gap-1.5 text-xs text-gray-400 dark:text-gray-500 lg:mt-auto">
              <svg xmlns="http://www.w3.org/2000/svg" class="w-3.5 h-3.5 shrink-0" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd" />
              </svg>
              <span>{props.template.author}</span>
              <Show when={props.template.author_email}>
                <span class="text-gray-200 dark:text-gray-700">·</span>
                <a
                  href={`mailto:${props.template.author_email}`}
                  class="hover:text-blue-500 dark:hover:text-blue-400 transition-colors truncate"
                >
                  {props.template.author_email}
                </a>
              </Show>
            </div>
          </Show>
        </div>

        {/* Right: URL + Actions */}
        <div class="p-5 flex flex-col min-w-0 border-t border-gray-100 lg:border-t-0 lg:border-l dark:border-gray-800">
          <div>
            <div class="text-xs font-medium text-gray-400 dark:text-gray-500 mb-1.5">模板 URL</div>
            <div class="flex items-center gap-2 bg-gray-50 rounded-lg px-2 py-1.5 border border-gray-100 dark:bg-gray-950 dark:border-gray-800">
              <input
                value={templateUrl()}
                readonly
                class="px-0.5 text-xs text-gray-700 dark:text-gray-200 font-mono flex-1 min-w-0 bg-transparent border-none outline-none"
                onFocus={(e) => e.currentTarget.select()}
              />
              <CopyButton text={templateUrl()} />
            </div>
          </div>

          <div class="mt-4 flex flex-wrap gap-2">
          <Show when={configureUrl()}>
            <a
              href={configureUrl()!}
              target="_blank"
              rel="noopener noreferrer"
              class="inline-flex items-center gap-1 px-3 py-1.5 rounded-lg bg-indigo-500 hover:bg-indigo-600 text-white text-xs font-medium transition-colors dark:bg-indigo-600 dark:hover:bg-indigo-500"
            >
              <svg xmlns="http://www.w3.org/2000/svg" class="w-3.5 h-3.5" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M11.49 3.17c-.38-1.56-2.6-1.56-2.98 0a1.532 1.532 0 01-2.286.948c-1.372-.836-2.942.734-2.106 2.106.54.886.061 2.042-.947 2.287-1.561.379-1.561 2.6 0 2.978a1.532 1.532 0 01.947 2.287c-.836 1.372.734 2.942 2.106 2.106a1.532 1.532 0 012.287.947c.379 1.561 2.6 1.561 2.978 0a1.533 1.533 0 012.287-.947c1.372.836 2.942-.734 2.106-2.106a1.533 1.533 0 01.947-2.287c1.561-.379 1.561-2.6 0-2.978a1.532 1.532 0 01-.947-2.287c.836-1.372-.734-2.942-2.106-2.106a1.532 1.532 0 01-2.287-.947zM10 13a3 3 0 100-6 3 3 0 000 6z" clip-rule="evenodd" />
              </svg>
              配置
            </a>
          </Show>
          <Show when={props.template.website_url}>
            <a
              href={props.template.website_url!}
              target="_blank"
              rel="noopener noreferrer"
              class="inline-flex items-center gap-1 px-3 py-1.5 rounded-lg bg-gray-100 hover:bg-gray-200 text-gray-700 text-xs font-medium transition-colors dark:bg-gray-800 dark:hover:bg-gray-700 dark:text-gray-200"
            >
              <svg xmlns="http://www.w3.org/2000/svg" class="w-3.5 h-3.5" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M4.083 9h1.946c.089-1.546.383-2.97.837-4.118A6.004 6.004 0 004.083 9zM10 2a8 8 0 100 16A8 8 0 0010 2zm0 2c-.076 0-.232.032-.465.262-.238.234-.497.623-.737 1.182-.389.907-.673 2.142-.766 3.556h3.936c-.093-1.414-.377-2.649-.766-3.556-.24-.56-.5-.948-.737-1.182C10.232 4.032 10.076 4 10 4zm3.971 5c-.089-1.546-.383-2.97-.837-4.118A6.004 6.004 0 0115.917 9h-1.946zm-2.003 2H8.032c.093 1.414.377 2.649.766 3.556.24.56.5.948.737 1.182.233.23.389.262.465.262.076 0 .232-.032.465-.262.238-.234.498-.623.737-1.182.389-.907.673-2.142.766-3.556zm1.166 4.118c.454-1.147.748-2.572.837-4.118h1.946a6.004 6.004 0 01-2.783 4.118zm-6.268 0C6.412 13.97 6.118 12.546 6.03 11H4.083a6.004 6.004 0 002.783 4.118z" clip-rule="evenodd" />
              </svg>
              主页
            </a>
          </Show>
          <Show when={props.template.repository_url}>
            <a
              href={props.template.repository_url!}
              target="_blank"
              rel="noopener noreferrer"
              class="inline-flex items-center gap-1 px-3 py-1.5 rounded-lg bg-gray-100 hover:bg-gray-200 text-gray-700 text-xs font-medium transition-colors dark:bg-gray-800 dark:hover:bg-gray-700 dark:text-gray-200"
            >
              <svg xmlns="http://www.w3.org/2000/svg" class="w-3.5 h-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                <path d="M6 3v12" />
                <circle cx="6" cy="3" r="2" />
                <circle cx="6" cy="15" r="2" />
                <path d="M6 8h8a4 4 0 0 1 4 4v0" />
                <circle cx="18" cy="15" r="2" />
              </svg>
              仓库
            </a>
          </Show>
        </div>
        </div>
      </div>
    </div>
    </div>
  );
};

export default TemplateCard;
