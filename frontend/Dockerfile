FROM nginxinc/nginx-unprivileged:1.27 AS runtime

LABEL org.opencontainers.image.source=https://github.com/spyder007/unifi-client
LABEL org.opencontainers.image.description="Unifi Client Host"
LABEL org.opencontainers.image.licenses=MIT

COPY nginx/default.conf /etc/nginx/conf.d/default.conf
COPY output/ /usr/share/nginx/html
