import { faExclamationCircle } from "@fortawesome/free-solid-svg-icons/faExclamationCircle";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import React from "react";
import { store } from "../../store/store";
import { removeMessage } from "../../store/slices/UserMessage";
import "./AlertMessage.scss";

interface Props {
  /** The message string to display */
  message: string;

  /** The style of message to display. */
  messageType?: "warning" | "error" | "success" | "info";
}

/** An alert banner displayed at the top of the screen (just under the header bar). */

export const AlertMessage = (props: Props): JSX.Element => {
  const { message, messageType } = props;

  const clearMessage = () => {
    store.dispatch(
      removeMessage({
        message: message,
        messageType: messageType ?? "info",
      })
    );
  };

  let alertClassName = "alert-primary";
  let iconEl;
  switch (messageType) {
    case "warning": {
      iconEl = <FontAwesomeIcon icon={faExclamationCircle} />;
      alertClassName = "alert-warning";
      break;
    }
    case "error": {
      iconEl = <FontAwesomeIcon icon={faExclamationCircle} />;
      alertClassName = "alert-danger";
      break;
    }
    case "success":
    case "info":
    default: {
      iconEl = <FontAwesomeIcon icon={faExclamationCircle} />;
      alertClassName = "alert-primary";
      break;
    }
  }

  return (
    <div
      className={`alert ${alertClassName} alert-dismissible fade show mainsite-alert`}
      role="alert"
    >
      {iconEl}
      <span className="mx-2">{message}</span>
      <button
        type="button"
        className="btn-close"
        onClick={clearMessage}
      ></button>
    </div>
  );
};

AlertMessage.defaultProps = {
  messageType: "info",
};
