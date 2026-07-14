import { render, screen } from "@testing-library/react";
import { AlertMessage } from "./AlertMessage";
import { describe, it, expect } from "vitest";

describe("AlertMessage", () => {
  it("renders correct text with default class", async () => {
    render(<AlertMessage message="test" />);
    await screen.findByRole("alert");

    expect(screen.getByRole("alert")).toHaveTextContent("test");
    expect(screen.getByRole("alert")).toHaveClass("alert-primary");
  });
  it("renders warning class", async () => {
    render(<AlertMessage message="test" messageType="warning" />);
    await screen.findByRole("alert");
    expect(screen.getByRole("alert")).toHaveClass("alert-warning");
  });
  it("renders error class", async () => {
    render(<AlertMessage message="test" messageType="error" />);
    await screen.findByRole("alert");
    expect(screen.getByRole("alert")).toHaveClass("alert-danger");
  });
  it("renders success class", async () => {
    render(<AlertMessage message="test" messageType="success" />);
    await screen.findByRole("alert");
    expect(screen.getByRole("alert")).toHaveClass("alert-primary");
  });
});
