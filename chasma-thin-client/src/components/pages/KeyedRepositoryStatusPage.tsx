import { useParams } from "react-router-dom";
import RepositoryStatusPage from "./RepositoryStatusPage";

/**
 * The keyed repository status page. The key is the repository identifier.
 */
export const KeyedRepositoryStatusPage: React.FC = () => {
    const { repoId } = useParams<{ repoId: string }>();
    return <RepositoryStatusPage key={repoId} />;
};

export default KeyedRepositoryStatusPage;