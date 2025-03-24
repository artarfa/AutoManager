from flask import Flask, request, jsonify
from transformers import AutoModelForSequenceClassification, AutoTokenizer
import torch

app = Flask(__name__)

# Load the NLI model and tokenizer (using roberta-large-mnli).
model_name = "roberta-large-mnli"
tokenizer = AutoTokenizer.from_pretrained(model_name)
model = AutoModelForSequenceClassification.from_pretrained(model_name)

@app.route('/contradiction', methods=['POST'])
def check_contradiction():
    data = request.get_json()
    text1 = data.get('text1', '')
    text2 = data.get('text2', '')
    
    if not text1 or not text2:
        return jsonify({"error": "Both text1 and text2 are required"}), 400
    
    # Encode the pair of texts. The model expects a sequence pair.
    inputs = tokenizer.encode_plus(text1, text2, return_tensors='pt', truncation=True)
    outputs = model(**inputs)
    logits = outputs.logits.detach().numpy()[0]
    # For roberta-large-mnli the order of labels is: contradiction, neutral, entailment.
    scores = torch.softmax(torch.tensor(logits), dim=0).numpy()
    result = {
        "contradiction": float(scores[0]),
        "neutral": float(scores[1]),
        "entailment": float(scores[2])
    }
    return jsonify(result)

if __name__ == '__main__':
    # Run on port 5002 to avoid conflict with the similarity service.
    app.run(host='0.0.0.0', port=5002)
