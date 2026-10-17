# Deployment runbook

Browser -> **NGINX VPS** (57.129.162.51, TLS) -> **App VPS** (57.129.162.53:8080, Docker container) -> **DB VPS** (57.129.162.52:5432, PostgreSQL).
Canonical URL is https://markf.dev; `www.markf.dev`, `markf.io` and `www.markf.io` redirect to it.

## 1. DNS (Hover)
For both `markf.dev` and `markf.io`, create `A` records `@` and `www` -> `57.129.162.51`.
(Remove any existing parking/forwarding records. Add `AAAA` only if the NGINX VPS has IPv6.)

All servers use the default `ubuntu` user with `sudo`. The `ubuntu` user is also the CI deploy user.

## 2. App VPS (57.129.162.53)
```bash
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker ubuntu     # log out and back in for this to apply
sudo mkdir -p /opt/markfdev && sudo chown ubuntu: /opt/markfdev

# add the CI public key (ci_key.pub, see section 4) so GitHub Actions can SSH in
echo "<contents of ci_key.pub>" >> ~/.ssh/authorized_keys

# create /opt/markfdev/.env
nano /opt/markfdev/.env
#   POSTGRES_PASSWORD=<same value as on the DB VPS>
#   ADMIN_EMAIL=you@markf.dev
#   ADMIN_PASSWORD=<12+ chars>
chmod 600 /opt/markfdev/.env
```
Port 8080 must be reachable only from the NGINX VPS. Docker bypasses ufw, so restrict it with the
OVH edge firewall (allow TCP 8080 from 57.129.162.51 only), or:
```bash
sudo iptables -I DOCKER-USER -p tcp --dport 8080 ! -s 57.129.162.51 -j DROP
```
Persist iptables rules across reboots: `sudo apt install -y iptables-persistent && sudo netfilter-persistent save`.
Allow your SSH port (53679 on the app VPS) only as you need it. Only the NGINX VPS needs 80/443.

## 3. NGINX VPS (57.129.162.51)
First copy `deploy/nginx-bootstrap.conf` and `deploy/markf.dev.conf` from the repo to the server (e.g. `scp deploy/*.conf ubuntu@57.129.162.51:~/`), then:
```bash
sudo apt update && sudo apt install -y nginx certbot
sudo mkdir -p /var/www/certbot
sudo cp ~/nginx-bootstrap.conf /etc/nginx/conf.d/markf.dev.conf
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t && sudo systemctl reload nginx

sudo certbot certonly --webroot -w /var/www/certbot --cert-name markf.dev \
  -d markf.dev -d www.markf.dev -d markf.io -d www.markf.io \
  --deploy-hook "systemctl reload nginx"

sudo cp ~/markf.dev.conf /etc/nginx/conf.d/markf.dev.conf
sudo nginx -t && sudo systemctl reload nginx
```
Open 80 and 443 in the firewall. Certbot's systemd timer handles renewal.
DNS must already point at this server before running certbot.

## 4. GitHub
The workflow `.github/workflows/deploy.yml` builds the image, pushes it to GHCR, copies `deploy/docker-compose.yml` to `/opt/markfdev` and runs `docker compose up -d` over SSH.
- Make the GHCR package public after the first push (or run `docker login ghcr.io` on the app VPS).
- Repository secrets:
  - `DEPLOY_HOST` = `57.129.162.53`
  - `DEPLOY_USER` = `ubuntu`
  - `DEPLOY_SSH_KEY` = private key (`ssh-keygen -t ed25519 -f ci_key`; public half goes on the VPS)
  - `DEPLOY_PORT` = `53679` (the app VPS's SSH port)
  - `DEPLOY_KNOWN_HOSTS` = the app VPS host key, in the non-standard-port format. Easiest way, run on the app VPS:
    ```bash
    echo "[57.129.162.53]:53679 $(cut -d' ' -f1,2 /etc/ssh/ssh_host_ed25519_key.pub)"
    ```
    (Windows' built-in `ssh-keyscan` is too old for OpenSSH 10. From WSL or Git Bash, `ssh-keyscan -t ed25519 -p 53679 57.129.162.53` also works.)

## 5. Database VPS (57.129.162.52)
PostgreSQL runs in Docker here, separate from the app. Do this **before** the first deploy.
```bash
curl -fsSL https://get.docker.com | sudo sh
sudo mkdir -p /opt/markfdev-db/certs && sudo chown -R ubuntu: /opt/markfdev-db
cd /opt/markfdev-db
# copy deploy/db/docker-compose.yml here (e.g. scp), then:
echo "POSTGRES_PASSWORD=$(openssl rand -base64 32 | tr -d '/+=')" > .env && chmod 600 .env

# self-signed certificate so app<->db traffic is encrypted (postgres image runs as uid 999)
openssl req -new -x509 -days 3650 -nodes -subj "/CN=57.129.162.52" \
  -addext "subjectAltName=IP:57.129.162.52" \
  -keyout certs/server.key -out certs/server.crt
chmod 600 certs/server.key
sudo chown 999:999 certs/server.key certs/server.crt   # do this last: ubuntu can't modify the files afterwards

sudo docker compose up -d
sudo docker compose ps
```
Copy the generated `POSTGRES_PASSWORD` into `/opt/markfdev/.env` on the app VPS.

**Firewall (important):** the database is reachable from the internet unless you block it. Allow TCP 5432 only from
57.129.162.53, using the OVH edge firewall, or (Docker bypasses ufw):
```bash
sudo iptables -I DOCKER-USER -p tcp --dport 5432 ! -s 57.129.162.53 -j DROP
```
Persist the rule: `sudo apt install -y iptables-persistent && sudo netfilter-persistent save`.

The app verifies this certificate (`SSL Mode=VerifyFull`), so it must be copied to the app VPS **before the first deploy**.
Run this on your PC (adjust the DB VPS SSH port if it isn't 22):
```bash
scp -P <db-ssh-port> ubuntu@57.129.162.52:/opt/markfdev-db/certs/server.crt ./db-ca.crt
scp -P 53679 ./db-ca.crt ubuntu@57.129.162.53:/opt/markfdev/db-ca.crt
```
The certificate is valid for 10 years and is tied to the IP `57.129.162.52`. If you regenerate it, copy the new `db-ca.crt` to the app VPS and run `sudo docker compose restart` there.
## 6. App behaviour and admin
- On startup the app applies EF Core migrations, seeds placeholder content if the database is empty, and creates the admin user from `ADMIN_EMAIL` / `ADMIN_PASSWORD` if it doesn't exist. Changing `ADMIN_PASSWORD` later does not change an existing user's password.
- The app will fail to start if the DB VPS is unreachable. Docker restarts it until the database is available.
- Edit content at https://markf.dev/admin (sign in at `/login`).
- Back up (on the DB VPS): `sudo docker compose exec -T db pg_dump -U markfdev markfdev > backup.sql` (run in `/opt/markfdev-db`)

## Local development
```
docker run -d --name markfdev-pg -e POSTGRES_USER=markfdev -e POSTGRES_PASSWORD=dev-only-password -e POSTGRES_DB=markfdev -p 5433:5432 postgres:17
dotnet user-secrets set "Admin:Email" "admin@example.com"
dotnet user-secrets set "Admin:Password" "a-long-dev-password"
dotnet run
```
New migration: `dotnet ef migrations add <Name>`.




