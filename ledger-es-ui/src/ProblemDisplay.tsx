import { Fragment, memo } from "react";
import { ProblemDetails } from "./problem";
import "./ProblemDisplay.css";

export interface ProblemDisplayProps {
  problem: ProblemDetails;
}

const ProblemDisplay = memo(function ProblemDisplay({ problem }: ProblemDisplayProps) {
  const errorKeys = Object.keys(problem.errors ?? {});

  return (
    <div className="problem-summary">
      <p>{problem.title}</p>
      <ProblemItemDisplay errorKey="" problem={problem} />
      {errorKeys.length > 0 && (
        <dl>
          {errorKeys.map((e) => (
            <Fragment key={e}>
              <dt>{e}</dt>
              <dd>
                <ProblemItemDisplay errorKey={e} problem={problem} />
              </dd>
            </Fragment>
          ))}
        </dl>
      )}
    </div>
  );
});

export default ProblemDisplay;

export interface ProblemItemDisplayProps {
  errorKey: string;
  problem: ProblemDetails;
}

export const ProblemItemDisplay = memo(function ProblemItemDisplay({
  errorKey,
  problem,
}: ProblemItemDisplayProps) {
  const errors = (problem.errors ?? {})[errorKey];
  if (!errors) return null;

  if (errors.length == 1) {
    return <p className="problem-item">{errors[0]}</p>;
  }

  return (
    <ul>
      {errors.map((e, i) => (
        <li className="problem-item" key={i}>{e}</li>
      ))}
    </ul>
  );
});
