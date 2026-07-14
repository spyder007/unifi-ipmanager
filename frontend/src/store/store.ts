import { configureStore, AnyAction } from "@reduxjs/toolkit";
import { useDispatch } from "react-redux";
import { ThunkAction, ThunkDispatch } from "redux-thunk";
import { rootReducer, RootState } from "./rootReducer";

export const store = configureStore({
  reducer: rootReducer,
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: false,
    }),
});

type AppState = ReturnType<typeof rootReducer>;
type TypedDispatch<T> = ThunkDispatch<T, any, AnyAction>;
export type AppDispatch = typeof store.dispatch;

export type AppThunk = ThunkAction<void, RootState, null, AnyAction>;

export const useAppDispatch = () => useDispatch<TypedDispatch<AppState>>();
