import { API_URL } from "@/lib/api";

/**
 * Passes a request through to the backend, carrying the auth cookie in both directions.
 * The browser therefore only ever talks to this origin.
 */
async function proxy(request: Request, path: string[]): Promise<Response> {
  const incoming = new URL(request.url);
  const target = `${API_URL}/${path.join("/")}${incoming.search}`;

  const headers = new Headers();
  for (const name of ["content-type", "accept", "cookie"]) {
    const value = request.headers.get(name);
    if (value) headers.set(name, value);
  }

  const upstream = await fetch(target, {
    method: request.method,
    headers,
    body: request.method === "GET" || request.method === "HEAD" ? undefined : await request.text(),
    redirect: "manual",
    cache: "no-store",
  });

  const response = new Headers();
  const contentType = upstream.headers.get("content-type");
  if (contentType) response.set("content-type", contentType);
  // getSetCookie keeps the headers separate; a plain get() would join them into one broken value.
  for (const cookie of upstream.headers.getSetCookie()) {
    response.append("set-cookie", cookie);
  }

  return new Response(upstream.body, { status: upstream.status, headers: response });
}

type Context = { params: Promise<{ path: string[] }> };

export async function GET(request: Request, { params }: Context) {
  return proxy(request, (await params).path);
}

export async function POST(request: Request, { params }: Context) {
  return proxy(request, (await params).path);
}

export async function DELETE(request: Request, { params }: Context) {
  return proxy(request, (await params).path);
}
