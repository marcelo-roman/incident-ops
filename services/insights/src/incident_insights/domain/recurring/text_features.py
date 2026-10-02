import warnings
from dataclasses import dataclass

import numpy as np
from scipy.sparse import csr_matrix
from sklearn.cluster import KMeans
from sklearn.decomposition import TruncatedSVD
from sklearn.exceptions import ConvergenceWarning
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics import silhouette_score
from sklearn.preprocessing import normalize
from threadpoolctl import threadpool_limits

MIN_DOCUMENTS = 3
SEMANTIC_DIMENSIONS = 100
MIN_SEMANTIC_DIMENSIONS = 2
DENSE_DOCUMENT_THRESHOLD = 20
WORD_TOKEN = r"(?u)\b[^\W\d_][\w-]+\b"


@dataclass(frozen=True)
class ClusteringOptions:
    max_clusters: int = 25
    min_cluster_size: int = 2
    label_terms: int = 3
    sample_titles: int = 3
    random_state: int = 42
    kmeans_restarts: int = 5


@dataclass(frozen=True)
class TextFeatures:
    tfidf: csr_matrix
    terms: np.ndarray
    embedding: np.ndarray


@dataclass(frozen=True)
class Clustering:
    labels: np.ndarray
    clusters: int
    silhouette: float


def extract_features(documents: list[str], random_state: int) -> TextFeatures | None:
    if len(documents) < MIN_DOCUMENTS:
        return None
    vectorizer = _vectorizer(len(documents))
    try:
        tfidf: csr_matrix = vectorizer.fit_transform(documents)
    except ValueError:
        return None
    return TextFeatures(
        tfidf=tfidf,
        terms=np.asarray(vectorizer.get_feature_names_out()),
        embedding=_embed(tfidf, random_state),
    )


def choose_clustering(embedding: np.ndarray, options: ClusteringOptions) -> Clustering | None:
    upper = min(options.max_clusters, embedding.shape[0] - 1)
    with threadpool_limits(limits=1):
        candidates = [_cluster(embedding, k, options) for k in range(2, upper + 1)]
    scored = [candidate for candidate in candidates if candidate is not None]
    if not scored:
        return None
    return max(scored, key=lambda candidate: candidate.silhouette)


def _vectorizer(documents: int) -> TfidfVectorizer:
    frequent_term_cutoff = 1.0
    if documents >= DENSE_DOCUMENT_THRESHOLD:
        frequent_term_cutoff = 0.5
    return TfidfVectorizer(
        stop_words="english",
        token_pattern=WORD_TOKEN,
        ngram_range=(1, 2),
        min_df=min(2, documents),
        max_df=frequent_term_cutoff,
        sublinear_tf=True,
    )


def _embed(tfidf: csr_matrix, random_state: int) -> np.ndarray:
    dimensions = min(SEMANTIC_DIMENSIONS, tfidf.shape[1] - 1, tfidf.shape[0] - 1)
    if dimensions < MIN_SEMANTIC_DIMENSIONS:
        return np.asarray(normalize(tfidf.toarray()))
    svd = TruncatedSVD(n_components=dimensions, random_state=random_state)
    with np.errstate(divide="ignore", invalid="ignore"):
        reduced = svd.fit_transform(tfidf)
    return np.asarray(normalize(reduced))


def _cluster(embedding: np.ndarray, k: int, options: ClusteringOptions) -> Clustering | None:
    model = KMeans(n_clusters=k, n_init=options.kmeans_restarts, random_state=options.random_state)
    with warnings.catch_warnings():
        warnings.simplefilter("ignore", ConvergenceWarning)
        labels = np.asarray(model.fit_predict(embedding))
    distinct = len(np.unique(labels))
    if distinct < 2 or distinct >= embedding.shape[0]:
        return None
    score = float(silhouette_score(embedding, labels, metric="cosine"))
    return Clustering(labels=labels, clusters=k, silhouette=score)
