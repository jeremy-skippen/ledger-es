import { useMemo, useState } from "react";
import { v4 as uuidv4 } from "uuid";
import Modal, { ModalBody, ModalFooter, ModalHeader } from "./Modal";
import { openLedger } from "./ledger";
import { ProblemDetails } from "./problem";
import ProblemDisplay, { ProblemItemDisplay } from "./ProblemDisplay";

export interface AddLedgerModalProps {
  onClose: () => void;
}

export default function AddLedgerModal({ onClose }: AddLedgerModalProps) {
  const ledgerId = useMemo(() => uuidv4(), []);
  const [ledgerName, setLedgerName] = useState<string>("");
  const [loading, setLoading] = useState<boolean>(false);
  const [problems, setProblems] = useState<ProblemDetails>();

  const onAdd = () => {
    setLoading(true);
    setProblems(undefined);
    openLedger({ ledgerId, ledgerName })
      .then(onClose)
      .catch((problem) => {
        setLoading(false);
        setProblems(problem);
      });
  };

  return (
    <Modal>
      <ModalHeader>
        <button className="close" type="button" onClick={onClose}>
          &times;
        </button>
        <h2>Add Ledger</h2>
      </ModalHeader>
      <ModalBody>
        {problems && <ProblemDisplay problem={problems} />}
        <dl className="input-group">
          <dt>Ledger Id</dt>
          <dd>
            <div className="input">{ledgerId}</div>
          </dd>
          <dt>Ledger Name</dt>
          <dd>
            <input
              type="text"
              className="input"
              value={ledgerName}
              disabled={loading}
              onChange={(e) => setLedgerName(e.currentTarget.value)}
            />
            {problems && (
              <ProblemItemDisplay errorKey="LedgerName" problem={problems} />
            )}
          </dd>
        </dl>
      </ModalBody>
      <ModalFooter>
        <button type="button" disabled={loading} onClick={onAdd}>
          Add Ledger
        </button>
      </ModalFooter>
    </Modal>
  );
}
