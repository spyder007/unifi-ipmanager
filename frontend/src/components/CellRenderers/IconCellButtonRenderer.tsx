import React from "react";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";

export const IconCellButtonRenderer: React.FunctionComponent<any> = (
  props: any
): JSX.Element => {
  const btnClickedHandler = (): void => {
    if (props.clicked) {
      props.clicked(props.value);
    }
  };
  return <FontAwesomeIcon icon={props.icon} onClick={btnClickedHandler} />;
};
