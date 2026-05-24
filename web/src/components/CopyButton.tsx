import type { Component } from 'solid-js';
import { createSignal } from 'solid-js';

const CopyButton: Component<{ text: string }> = (props) => {
  const [copied, setCopied] = createSignal(false);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(props.text);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      // fallback: ignore
    }
  };

  return (
    <button
      onClick={handleCopy}
      class={`shrink-0 px-3 py-1.5 rounded-md text-xs font-medium transition-colors ${
        copied()
          ? 'bg-green-500 text-white dark:bg-green-600 border border-green-500 dark:border-green-600'
          : 'bg-white hover:bg-gray-100 text-gray-600 border border-gray-200 dark:bg-gray-900 dark:hover:bg-gray-800 dark:text-gray-300 dark:border-gray-700'
      }`}
    >
      {copied() ? '已复制' : '复制'}
    </button>
  );
};

export default CopyButton;
