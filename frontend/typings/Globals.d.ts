declare module '*.module.css';

interface Window {
  Panelarr: {
    apiKey: string;
    instanceName: string;
    theme: string;
    urlBase: string;
    version: string;
    isProduction: boolean;
  };
}
