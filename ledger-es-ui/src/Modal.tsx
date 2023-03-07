import { ReactNode } from "react";
import "./Modal.css";

export interface ModalProps {
  children: ReactNode;
}

export default function Modal({ children }: ModalProps) {
  return (
    <div className="modal-backdrop">
      <div className="modal">{children}</div>
    </div>
  );
}

export function ModalHeader({ children }: ModalProps) {
  return <div className="modal-header">{children}</div>;
}

export function ModalBody({ children }: ModalProps) {
  return <div className="modal-body">{children}</div>;
}

export function ModalFooter({ children }: ModalProps) {
  return <div className="modal-footer">{children}</div>;
}
