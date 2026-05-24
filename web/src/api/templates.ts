import type { HttpApiResponse, ListTemplateResData } from '../types/api';

export async function fetchTemplates(): Promise<ListTemplateResData> {
  const res = await fetch('/api/templates');
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  const json: HttpApiResponse<ListTemplateResData> = await res.json();
  if (json.status !== 200) throw new Error(json.message);
  return json.data;
}
