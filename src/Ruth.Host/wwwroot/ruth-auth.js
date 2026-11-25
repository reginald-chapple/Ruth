export async function cookieLogin(userNameOrEmail, password, rememberMe) {
  const response = await fetch("/api/auth/cookie-login", {
    method: "POST",
    credentials: "same-origin",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ userNameOrEmail, password, rememberMe }),
  });

  if (response.ok) {
    return { ok: true, error: null };
  }

  try {
    const problem = await response.json();
    return { ok: false, error: problem?.detail ?? "Invalid username or password." };
  } catch {
    return { ok: false, error: "Invalid username or password." };
  }
}

export async function cookieLogout() {
  await fetch("/api/auth/cookie-logout", {
    method: "POST",
    credentials: "same-origin",
  });
}
