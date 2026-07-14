import "./App.css";
import { Provider } from "react-redux";
import { store } from "./store/store";
import AzureAuthenticationContext from "./utils/authProvider";
import { AppRouter } from "./components/AppRouter";
import { MsalProvider } from "@azure/msal-react";

function App() {
  return (
    <Provider store={store}>
      <MsalProvider
        instance={AzureAuthenticationContext.getInstance().pcaInstance}
      >
        <AppRouter />
      </MsalProvider>
    </Provider>
  );
}

export default App;
