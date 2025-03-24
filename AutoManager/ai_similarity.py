from flask import Flask, request, jsonify
from sentence_transformers import SentenceTransformer, util

app = Flask(__name__)

# Load the model once when the service starts.
model = SentenceTransformer('all-MiniLM-L6-v2')

@app.route('/similarity', methods=['POST'])
def similarity():
    data = request.get_json()
    text1 = data.get('text1', '')
    text2 = data.get('text2', '')
    if not text1 or not text2:
        return jsonify({"error": "Both text1 and text2 are required"}), 400
    
    # Compute embeddings and cosine similarity.
    embedding1 = model.encode(text1, convert_to_tensor=True)
    embedding2 = model.encode(text2, convert_to_tensor=True)
    cosine_score = util.cos_sim(embedding1, embedding2).item()
    
    return jsonify({"similarity": cosine_score})

if __name__ == '__main__':
    # Run on port 5001 to avoid conflicts.
    app.run(host='0.0.0.0', port=5001)