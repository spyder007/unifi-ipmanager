import React from "react";
import { store } from "../store/store";
import { showMessage } from "../store/slices/UserMessage";

export const Home: React.FunctionComponent = () => {
  const testMessage = (): void => {
    store.dispatch(
      showMessage({
        messageType: "info",
        message: "HI!!!!",
      })
    );
  };

  return (
    <div>
      <section className="py-5">
        <div className="container my-5">
          <div className="row justify-content-center">
            <div className="col-lg-6">
              <h2>Public Page</h2>
              <p className="lead">This page is public</p>
              <button onClick={testMessage} className="btn btn-primary">
                Test Alert Message
              </button>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
};
