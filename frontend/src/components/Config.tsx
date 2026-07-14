import createConfig from "@spydersoft/react-runtime-config";

export const { useConfig: useApiConfig, getConfig: getApiConfig } =
  createConfig({
    namespace: "unifi_client_api",
    schema: {
      backend: {
        type: "string",
        description: "Unifi IP Manager Backend Url",
        default: "http://localhost:3000",
      },
    },
  });

export const { useConfig: useAuthConfig, getConfig: getAuthConfig } =
  createConfig({
    namespace: "unifi_client_auth",
    schema: {
      authority: {
        type: "string",
        description: "Auth Authority Url",
        default: "http://localhost:3001",
      },
      clientId: {
        type: "string",
        description: "Auth Client Id",
        default: "client_id",
      },
      redirectUri: {
        type: "string",
        description: "Auth Redirect Uri",
        default: "http://localhost:3000",
      },
    },
  });
