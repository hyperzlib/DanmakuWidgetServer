export interface TemplateManifest {
  name: string;
  description: string;
  version?: string | null;
  preview_img?: string | null;
  author?: string | null;
  author_email?: string | null;
  repository_url?: string | null;
  website_url?: string | null;
  configure_url?: string | null;
  template_url: string;
}

export interface ListTemplateResData {
  base_url_list: string[];
  templates: TemplateManifest[];
}

export interface HttpApiResponse<T> {
  status: number;
  message: string;
  warnings?: string[];
  error_trace?: string | null;
  data: T;
}
