// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { FluentProvider, webLightTheme } from "@fluentui/react-components";
import Chat from "./Chat.tsx";

import styles from "./App.module.css";

function App() {
  return (
    <FluentProvider theme={webLightTheme}>
      process.env.services__backend__https__0
      <div className={styles.appContainer}>
        <Chat style={{ flex: 1 }} />       
      </div>
    </FluentProvider>
  );
}

export default App;
