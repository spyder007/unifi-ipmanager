import { combineReducers } from "redux";
import { userMessageReducer } from "./slices/UserMessage";
import { userSliceReducer } from "./slices/User";
import { clientListReducer } from "./slices/ClientList";

export const rootReducer = combineReducers({
  userMessage: userMessageReducer,
  user: userSliceReducer,
  clientList: clientListReducer,
});

export type RootState = ReturnType<typeof rootReducer>;
