namespace TLinkProxy.Base.Models
{
    public sealed class Model_Info
    {
        public int deepseek2attentionhead_count { get; set; }
        public int deepseek2attentionhead_count_kv { get; set; }
        public int deepseek2attentionkey_length { get; set; }
        public int deepseek2attentionkv_lora_rank { get; set; }
        public float deepseek2attentionlayer_norm_rms_epsilon { get; set; }
        public int deepseek2attentionvalue_length { get; set; }
        public int deepseek2block_count { get; set; }
        public int deepseek2context_length { get; set; }
        public int deepseek2embedding_length { get; set; }
        public int deepseek2expert_count { get; set; }
        public int deepseek2expert_feed_forward_length { get; set; }
        public int deepseek2expert_shared_count { get; set; }
        public int deepseek2expert_used_count { get; set; }
        public int deepseek2expert_weights_scale { get; set; }
        public int deepseek2feed_forward_length { get; set; }
        public int deepseek2leading_dense_block_count { get; set; }
        public int deepseek2ropedimension_count { get; set; }
        public int deepseek2ropefreq_base { get; set; }
        public int deepseek2ropescalingfactor { get; set; }
        public int deepseek2ropescalingoriginal_context_length { get; set; }
        public string deepseek2ropescalingtype { get; set; }
        public float deepseek2ropescalingyarn_log_multiplier { get; set; }
        public int deepseek2vocab_size { get; set; }
        public string generalarchitecture { get; set; }
        public int generalfile_type { get; set; }
        public long generalparameter_count { get; set; }
        public int generalquantization_version { get; set; }
        public bool tokenizerggmladd_bos_token { get; set; }
        public bool tokenizerggmladd_eos_token { get; set; }
        public int tokenizerggmlbos_token_id { get; set; }
        public int tokenizerggmleos_token_id { get; set; }
        public object tokenizerggmlmerges { get; set; }
        public string tokenizerggmlmodel { get; set; }
        public int tokenizerggmlpadding_token_id { get; set; }
        public string tokenizerggmlpre { get; set; }
        public object tokenizerggmltoken_type { get; set; }
        public object tokenizerggmltokens { get; set; }
    }
}
