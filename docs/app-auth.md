# GitHub App Authentication

This guide covers how to authenticate Orca using a GitHub App. This is the recommended method for CI pipelines and automation where a personal token is not appropriate.

## When to use GitHub App auth

| Context | Recommended method |
|---|---|
| Local development | `gh auth login` or `orca auth pat` |
| CI pipeline | GitHub App via `orca auth app` |
| Shared automation | GitHub App via `orca auth app` |

---

## 1. Create a GitHub App

1. Go to your organisation settings: `https://github.com/organizations/<org>/settings/apps`
2. Click **New GitHub App**.
3. Fill in the required fields:
   - **GitHub App name** — e.g. `orca-bot`
   - **Homepage URL** — any URL (e.g. your repo URL)
   - **Webhook** — uncheck *Active* (Orca does not use webhooks)
4. Set the following **Repository permissions**:
   - **Issues** — Read & write
   - **Pull requests** — Read & write
   - **Contents** — Read (required by `gh` for some operations)
5. Set the following **Organisation permissions**:
   - **Projects** — Read & write
6. Under **Where can this GitHub App be installed?**, select **Only on this account**.
7. Click **Create GitHub App**.

---

## 2. Generate a private key

1. On the App settings page, scroll to **Private keys**.
2. Click **Generate a private key**.
3. A `.pem` file is downloaded. Store it somewhere safe — you cannot download it again.

Orca accepts both PKCS#1 (`-----BEGIN RSA PRIVATE KEY-----`) and PKCS#8 (`-----BEGIN PRIVATE KEY-----`) format keys.

---

## 3. Install the App on your organisation

1. From the App settings page, click **Install App** in the left sidebar.
2. Click **Install** next to your organisation.
3. Choose **All repositories** or select specific repositories that Orca will manage.
4. Click **Install**.

---

## 4. Find your App ID and Installation ID

**App ID**

On the App settings page (General tab), the App ID is shown near the top — it is a plain integer, e.g. `123456`.

**Installation ID**

After installing the App, navigate to:

```
https://github.com/organizations/<org>/settings/installations
```

Click **Configure** next to your App. The installation ID is the number at the end of the URL:

```
https://github.com/organizations/<org>/settings/installations/98765432
                                                               ^^^^^^^^
                                                               installation ID
```

Alternatively, retrieve it via the GitHub API:

```bash
# Replace APP_ID and JWT with your values
gh api /app/installations \
  -H "Authorization: Bearer <jwt>" \
  -H "Accept: application/vnd.github+json" \
  --jq '.[].id'
```

---

## 5. Configure Orca

```bash
orca auth app \
  --app-id <APP_ID> \
  --key /path/to/private-key.pem \
  --installation-id <INSTALLATION_ID>
```

Orca will:
1. Save the config to `~/.config/orca/auth.json`.
2. Generate a short-lived JWT from the App ID and private key.
3. Exchange the JWT for an installation token via the GitHub API.
4. Validate the token with `gh auth status`.

On success you will see:

```
GitHub App config saved and validated.
```

---

## 6. Using Orca in CI

In a CI environment, store credentials as secrets and pass them at runtime. Orca reads from `~/.config/orca/auth.json`, so run the `auth app` command as part of your setup step:

```yaml
- name: Configure Orca auth
  run: |
    orca auth app \
      --app-id ${{ secrets.ORCA_APP_ID }} \
      --key ${{ secrets.ORCA_PRIVATE_KEY_PATH }} \
      --installation-id ${{ secrets.ORCA_INSTALLATION_ID }}
```

If you prefer to avoid writing the key to disk, export the PEM content as an environment variable and write it to a temporary file:

```yaml
- name: Configure Orca auth
  env:
    ORCA_PRIVATE_KEY: ${{ secrets.ORCA_PRIVATE_KEY }}
  run: |
    echo "$ORCA_PRIVATE_KEY" > /tmp/orca-key.pem
    orca auth app \
      --app-id ${{ secrets.ORCA_APP_ID }} \
      --key /tmp/orca-key.pem \
      --installation-id ${{ secrets.ORCA_INSTALLATION_ID }}
    rm /tmp/orca-key.pem
```

---

## Troubleshooting

**`Failed to generate JWT`**

- Ensure the `.pem` file is the private key, not the public key.
- The file must begin with `-----BEGIN RSA PRIVATE KEY-----` or `-----BEGIN PRIVATE KEY-----`.

**`GitHub API returned 401`**

- The JWT has expired or the App ID does not match the key. Re-run `orca auth app` to generate a fresh config.

**`GitHub API returned 404`**

- The installation ID is wrong, or the App is not installed on the organisation. Verify the installation ID in the organisation settings.

**`GitHub API returned 403`**

- The App lacks the required permissions. Review Step 1 and update the App's permissions, then re-install.

**`PAT saved but validation failed`**

- `gh` is not installed or not on `PATH`. Install the [GitHub CLI](https://cli.github.com/) and ensure it is accessible.
